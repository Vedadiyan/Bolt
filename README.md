# Bolt 
*Bolt is a lightweight micro ORM which supports strongly-typed queries.*  
Please note that Bolt does not generate any code and it is can only be used as a database-first solution. 

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
|

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



