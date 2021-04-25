# Bolt 
*Bolt is a lightweight micro ORM which supports strongly-typed queries.*  
Please note that:
 - Bolt does NOT generate any code and it can only be used as a database-first solution
 - Bolt is mainly focused on making queries and it has little support for running add, delete, or update commands
 - Bolt does NOT come with a change tracker and will never have one

## Getting Started 
Bolt requires strongly typed data models to produce a SQL query. However, Bolt can also handle certain select parameters that cannot be included in the data model on the fly. This documentation will dive into all features of Bolt ORM in detail. 

### Model Definition
Bolt is an annotation driven framework. To define a data model, a class together with its properties can be decorated with Bolt attributes. 

|Attribute| Description  | Example
|--|--|--|
| Column | Associates a property with a physical column  | `[Column("first_name")] public string FirstName {get; set;}`
| Table| Associates a class with a physical table | `[Table("users")] public class User { ... }`
| PrimaryKey | Marks a property as a non-auto-incremented Primary Key| `[PrimaryKey] public string Id {get; set;}`
| SurrogateKey | Marks a property as an auto-incremented or database-generated key | `[SurrogateKey] public long Id {get; set;}`
|UniqueKey| Marks a property as a unique key | `[UniqueKey] public string Email {get; set;}`
|CompositePrimaryKey| Adds a property to a composite primary key group | `[CompositePrimaryKey("G1")] public string Email {get; set;}`

Bolts also requires data model registration via the `DSS` helper. 

Example: 

    [Table("users")]
    public class User {
	  
	    [SurrogateKey]
	    [Column("id")]
	    public long Id {get; set;}
	  
	    [Column("full_name")]
	    public string FullName {get; set;}
	  
	    [Unique]
	    [Column("email")]
	    public string Email {get; set;}

		static User() {
			DSS.RegisterTableStructure<User>();
		}
	} 

### Query Mechanism 
Bolt does not require definition of navigational properties. Instead, it leaves relational mapping to the developer (via explicit joins) and returns correlated records as tuples! 

Example: 

    Query<User> sampleQuery = 
	    new Query<User>()
		    .Join<User, Like>(x=> x.Left.Id == x.Right.UserId)
		    // if required, you can use a named tuple
		    // example: .Where<(User usr, Like like)>(x=> x.like.Date > DBO.Function<DateTime>("GETDATE()"))
		    .Where<Like>(x=> x.Date == /* Get current date at (database) server side */ DBO.Function<DateTime>("GETDATE()"))
		    .Select();
Bolt does not know against which database it should run the query in advance, and similar to conventional database drivers, it requires a connection string. 

Example: 

    ResultSet resultSet = new ResultSet(sampleQuery);
    await resultSet.LoadAsync("[connection string]");
    var result= resultSet.ToList();
Each item of the `result` list comes with a `GetEntity<T()` method which allows for accessing the desired record. 

Example: 

    foreach(var item in result) {
	    var user = item.GetEntity<User>();
	    var like = item.GetEntity<Like>();
	}

---
To be continued

 

