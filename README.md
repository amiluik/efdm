# EFDM
Entity framework data manager

## Usage
You can find working example in `EFDM.Test.*` projects in `Test` folder

### Base entities
Inherit own entities from EFDM.Core.Models.Domain

```c#
public class Group : DictIntDeletableEntity {
	public int TypeId { get; set; }
	public virtual GroupType Type { get; set; }
	public virtual ICollection<GroupUser> Users { get; set; }
}
```

### Query entities
```c#
public class GroupQuery : DictIntDeletableDataQuery<Group> {
	public int[] UserIds { get; set; }
	public int[] TypeIds { get; set; }

	public override IQueryFilter<Group> ToFilter() {
		var and = new QueryFilter<Group>();

		if (UserIds?.Any() == true)
			and.Add(x => x.Users.Any(xx => UserIds.Contains(xx.UserId)));

		if (TypeIds?.Any() == true)
			and.Add(x => TypeIds.Contains(x.TypeId));

		return base.ToFilter().Add(and);
	}
}
```

### Domain entity service
Create domain service class for entity

```c#
public class GroupService : DomainServiceBase<Group, GroupQuery, int, IRepository<Group, int>>, IGroupService {
        
	readonly IRepository<User, int> UserRepo;

	public GroupService(            
		IRepository<User, int> userRepo,
		IRepository<Group, int> repository,
		ILogger logger
	) : base(repository, logger) {

		UserRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
	}

	public void AddUser(int groupId, int userId) {
		Group group = GetById(groupId);

		User user = UserRepo.Fetch(new UserQuery {
			Ids = new[] { userId },
			IsDeleted = false,
			Includes = new[] { nameof(User.Groups) },
			Take = 1
		}, true).First();

		if (user.Groups.Any(e => e.GroupId == groupId))
			return;

		user.Groups.Add(new GroupUser { GroupId = groupId, UserId = userId });
		UserRepo.Save(user);
	}

	public void RemoveUser(int groupId, int userId) {
		Group group = GetById(groupId);

		User user = UserRepo.Fetch(new UserQuery {
			Ids = new[] { userId },
			Includes = new[] { nameof(User.Groups) },
			Take = 1
		}).FirstOrDefault();

		GroupUser groupUser = user?.Groups.FirstOrDefault(g => g.GroupId == groupId);
		if (groupUser == null)
			return;

		user.Groups.Remove(groupUser);
		UserRepo.Save(user);
	}
}
```

### Database context
Create own database context class inherited from `EFDM.Core.DAL.Providers.EFDMDatabaseContext`

```c#
public class TestDatabaseContext : EFDMDatabaseContext {

	#region fields & props

	public override int ExecutorId { get; protected set; } = UserVals.System.Id;

	#endregion fields & props

	#region dbsets
	
	public DbSet<Group> Groups { get; set; }	
	public DbSet<AuditGroupEvent> AuditGroupEvents { get; set; }
	public DbSet<AuditGroupProperty> AuditGroupProperties { get; set; }

	#endregion dbsets

	#region constructors

	public TestDatabaseContext(DbContextOptions<TestDatabaseContext> options,
		ILoggerFactory factory = null, IAuditSettings auditSettings = null)
		: base(options, factory, auditSettings) {
	}

	public TestDatabaseContext(string connectionString, IAuditSettings auditSettings = null)
		: base(connectionString, auditSettings) {
	}

	#endregion constructors

	#region context config

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {		
		base.OnConfiguring(optionsBuilder);		
	}

	protected override void OnModelCreating(ModelBuilder builder) {
		base.OnModelCreating(builder);
		builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
	}

	#endregion context config

	#region audit config

	public override void InitAuditMapping() {		
	}

	#endregion audit config
}
```

### Database context audit
Configure audit in own database context by overriding InitAuditMapping method

```c#
public override void InitAuditMapping() {
	Auditor.Map<Group, AuditGroupEvent, AuditGroupProperty>(
		(auditEvent, entry, eventEntity) => {
			eventEntity.ObjectId = entry.GetEntry().Entity.GetPropValue("Id").ToString();
		}
	);
	Auditor.SetEventCommonAction<IAuditEventBase<long>>((auditEvent, entry, eventEntity) => {
		eventEntity.ActionId = entry.Action;
		eventEntity.CreatedById = ExecutorId;
		eventEntity.ObjectType = entry.EntityType.Name;
		eventEntity.Created = DateTimeOffset.Now;

		Add(eventEntity);
		BaseSaveChanges();

		Func<IAuditPropertyBase<long, long>> createPropertyEntity = () => {
			var res = (Activator.CreateInstance(Auditor.GetPropertyType(entry.EntityType))) as IAuditPropertyBase<long, long>;
			res.AuditId = eventEntity.Id;
			return res;
		};
		Action<IAuditPropertyBase<long, long>> savePropertyEntity = (pe) => {
			if (string.IsNullOrEmpty(pe.Name))
				return;
			Add(pe);
			BaseSaveChanges();
		};
		switch (entry.Action) {
			case AuditStateActionVals.Insert:
				foreach (var columnVal in entry.ColumnValues) {
					var propertyEntity = createPropertyEntity();
					propertyEntity.Name = columnVal.Key;
					propertyEntity.NewValue = columnVal.Value.ToString();
					savePropertyEntity(propertyEntity);
				}
				break;
			case AuditStateActionVals.Delete:
				foreach (var columnVal in entry.ColumnValues) {
					var propertyEntity = createPropertyEntity();
					propertyEntity.Name = columnVal.Key;
					propertyEntity.OldValue = columnVal.Value.ToString();
					savePropertyEntity(propertyEntity);
				}
				break;
			case AuditStateActionVals.Update:
				foreach (var change in entry.Changes) {
					var propertyEntity = createPropertyEntity();
					propertyEntity.Name = change.ColumnName;
					propertyEntity.NewValue = change.NewValue.ToString();
					propertyEntity.OldValue = change.OriginalValue.ToString();
					savePropertyEntity(propertyEntity);
				}
				break;
			default:
				break;
		}
	});
}
```
## Examples
You can find examples in EFDM.Test.TestConsole project
