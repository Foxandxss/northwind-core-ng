# Steps for creating .NET Core Angular App

Here are some steps to follow to create a new Angular 8 application
with a .NET Core + EF backend, using Breeze to handle the data management.

We start with an empty directory, and create a new .NET Core solution
and a Angular 8 client app.

This is an opinionated approach -- the "productivity path" -- that 
[IdeaBlade](https://www.ideablade.com/) has found works well for many projects.  It includes generating entity classes for both server and client side.

We're using Visual Studio 2017 for the backend code, and VSCode for the frontend
Angular project.  Naturally you are free to use something else, and hopefully this
project will still be helpful.

## Create the directory

Our sample is going to have separate directories for client projects, server projects, and database scripts.  

We'll create subdirectories as we go, but start by creating the root directory.  We'll call it **NorthwindCore**.

## Create the database

It's common to build a new app using an existing database, so we will start by
creating the sample database.  The sample database we will use is a subset of the 
classic Microsoft sample db, "Northwind".  The scripts for creating it were copied
from the SQL tutorial site [dofactory](https://www.dofactory.com/sql/sample-database).

The scripts assume MS SQL Server or SQL Express.  If you are using a different database server, you will need to adapt the scripts accordingly.

1. Copy contents of the `dbscripts` folder from the sample repo into your own `dbscripts` folder.

2. In SQL Server Management Studio (or similar tool), create the database:

        CREATE DATABASE NorthwindCore
        GO
        USE NorthwindCore
        GO

3. Open and run the script to create the tables, `dbscripts\sample-model.sql`

4. Open and run the script to insert the data, `dbscripts\sample-data.sql`

## Create the .NET Core solution

Here we create the Visual Studio solution and the backend projects.  For this demo, we will have two projects: the model project, which implements the data model, and the server project, which implements the API.

1. Create the server project.  

    - In Visual Studio, select File / New / Project...
    - Choose project type .NET Core / **ASP.NET Core Web Application**
    - Set the project Name to **NorthwindServer**
    - Set the Location to the **NorthwindCore** directory that you created above
    - Make sure "Create directory for solution" is checked
    - Click OK, which takes you to the next dialog
    - Choose **ASP.NET Core 2.2** target
    - Choose **Empty** web application
    - Click OK

2. Create the model project

    - Right-click on the solution and choose Add / New Project... (or, from the top menu, select File / Add / New Project... )
    - Choose project type .NET Standard / **Class Library (.NET Standard).**
    - Set the project Name to **NorthwindModel**
    - Set the Location to the **NorwindCore/NorthwindServer** directory that was created for the solution
    - Click OK

3. Add a reference to the model project

    - Right-click on the **NorthwindServer** project and select Add / Reference...
    - Choose Projects / Solution and check **NorthwindModel**
    - Click OK
    - Set **NorthwindServer** as the startup project if it is not already.

4. Add Nuget packages to the server project.  These support the data API that we will create using Breeze.

    - Select File / Tools / Nuget Package Manager / Package Manager Console
    - Set Default project to **NorthwindServer**
    - `Install-Package Breeze.AspNetCore.NetCore`
    - `Install-Package Breeze.Persistence.EFCore`

5. Add Nuget packages to the model project.  These support using Entity Framework
for data access and data model creation.

    - Select File / Tools / Nuget Package Manager / Package Manager Console
    - Set Default project to **NorthwindModel**
    - `Install-Package Bricelam.EntityFrameworkCore.Pluralizer`
    - `Install-Package Microsoft.EntityFrameworkCore.SqlServer`

## Create the data model

We will create the data model classes from the database schema.  For this, we need a connection string for connecting to the database.  The commands below assume a local SQL Express instance; if you are using a separate server, you will need to change the Data Source and the security information in the connection string.

1. Select File / Tools / Nuget Package Manager / Package Manager Console
2. Set Default project to **NorthwindModel**
3. `Scaffold-DbContext "Data Source=.;Initial Catalog=NorthwindCore;Integrated Security=True;MultipleActiveResultSets=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -UseDatabaseNames -Force`
    - (The command above should all be on one line)

Now you should have a **Models** folder in the **NorthwindModel** project, which contains classes for each of the NorthwindCore database tables, and a `NorthwindCoreContext.cs` that contains the EF DbContext class for accessing the database.

If you change the database schema, you can re-run the Scaffold-DbContext command to re-build the model classes.

## Create the Persistence Manager

The PersistenceManager is a Breeze class that wraps the DbContext to provide Breeze data management.  We will create it in the **NorthwindServer** project

1. Right-click on the **NorthwindServer** project and select Add / Class...
2. Name the class `NorthwindCorePersistenceManager.cs` and click Add.  This will create the class file.
3. In the class file, remove the default `using` statements and replace them with 
```
using Breeze.Persistence.EFCore;
using NorthwindModel.Models;
```
4. Make the class extend EFPersistenceManager
```
public class NorthwindCorePersistenceManager : EFPersistenceManager<NorthwindCoreContext>
```
5. Add a constructor to create it from our DbContext
```
public NorthwindCorePersistenceManager(NorthwindCoreContext dbContext) : base(dbContext) {}
```

## Add a Breeze controller

We will add a controller class that will be the interface between the client and server.

1. Right-click on the **NorthwindServer** project and select Add / New Item...
2. Choose type **API Controller Class**
3. Set the Name to `BreezeController.cs`
4. Click Add.  This will create the controller class with some default methods.
5. Delete all the existing methods in the BreezeController class
6. Add attributes to the class so it will can perform Breeze queries:

```
using Breeze.AspNetCore;
using Breeze.Persistence;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NorthwindModel.Models;
using System.Linq;
...
  [Route("api/[controller]/[action]")]
  [BreezeQueryFilter]
  public class BreezeController : Controller
```

### Add the Persistence Manager to the BreezeController

Add a new `persistenceManager` field to the `BreezeController` class, and add a constructor that takes a NorthwindCoreContext and sets the `persistenceManager` field.  This will be called by dependency injection.
```
  private NorthwindCorePersistenceManager persistenceManager;
  public BreezeController(NorthwindCoreContext dbContext)
  {
      persistenceManager = new NorthwindCorePersistenceManager(dbContext);
  }
```

### Add query methods to the BreezeController

Add a HttpGet method returning `IQueryable<>` for each of the `Customer`, `Order`, `Product`, and `Supplier` types in the data model.  We won't do one for `OrderItem` because we will only query those with and `Order`
```
  [HttpGet]
  public IQueryable<Customer> Customers()
  {
      return persistenceManager.Context.Customer;
  }
  [HttpGet]
  public IQueryable<Order> Orders()
  {
      return persistenceManager.Context.Order;
  }
  [HttpGet]
  public IQueryable<Product> Products()
  {
      return persistenceManager.Context.Product;
  }
  [HttpGet]
  public IQueryable<Supplier> Suppliers()
  {
      return persistenceManager.Context.Supplier;
  }
```
### Add a SaveChanges method to the BreezeController

This HttpPost method will be called by the client to create/update/delete entities.
```
  [HttpPost]
  public ActionResult<SaveResult> SaveChanges([FromBody] JObject saveBundle)
  {
      return persistenceManager.SaveChanges(saveBundle);
  }
```
## Compile Time!

Now make sure the solution compiles.  Don't run it yet, there are a few more things to add.

## Add connection string

Add the connection string to the `appsettings.json` file
```
  "ConnectionStrings": {
    "NorthwindCore": "Data Source=.;Initial Catalog=NorthwindCore;Integrated Security=True;MultipleActiveResultSets=True"
  },
```

## Configure the Startup class

Edit the Startup.cs file in the **NorthwindServer** project.

We will need access to the `appsettings.json` configuration, so we will need to add a constructor to accept an IConfiguration instance:
```
        private IConfiguration configuration;
        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
```

In the `ConfigureServices` method, we need to 
1. Enable MVC, so our `BreezeController` class can be used to handle requests
2. Set JSON serialization options so the client-side Breeze can send and receive entities
3. Add an exception filter, so errors are communicated to the Breeze client
4. Add the DbContext to dependency injection, so our BreezeController can receive it

add some MVC options to let the Breeze client communicate with the server:
```
public void ConfigureServices(IServiceCollection services)
{
    var mvcBuilder = services.AddMvc();

    mvcBuilder.AddJsonOptions(opt => {
        // Set Breeze defaults for entity serialization
        var ss = JsonSerializationFns.UpdateWithDefaults(opt.SerializerSettings);
        if (ss.ContractResolver is DefaultContractResolver resolver)
        {
            resolver.NamingStrategy = null;  // remove json camelCasing; names are converted on the client.
        }
    });
    // Add Breeze exception filter to send errors back to the client
    mvcBuilder.AddMvcOptions(o => { o.Filters.Add(new GlobalExceptionFilter()); });

    // Add DbContext using connection string
    var connectionString = configuration.GetConnectionString("NorthwindCore");
    services.AddDbContext<NorthwindCoreContext>(options => options.UseSqlServer(connectionString));
}
```
In the `Configure` method, remove the existing `app.Run()` statement.  Then configure CORS and turn on MVC:
```
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // Allow any host - development only!
    app.UseCors(builder => builder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed((host) => true)
        .AllowCredentials()
    );

    app.UseMvc();
}
```

## Configure the Program class to generate metadata

To make client-side development easier, we are going to generate Breeze metadata from our entity model.  
Later we will use that metadata to generate TypeScript class representing our entity types.

When we run our .NET Core application from the command line, it starts in the `Main` method of `Program.cs`.
So we will change the `Main` method to produce metadata output, instead of starting the server, when we give it a "metadata" command-line parameter:
```
public static void Main(string[] args)
{
    if (args.Length > 0 && args[0].Contains("metadata"))
    {
        // Generate metadata and exit
        var optionsBuilder = new DbContextOptionsBuilder<NorthwindCoreContext>().UseInMemoryDatabase("foo", null);
        var dbContext = new NorthwindCoreContext(optionsBuilder.Options);
        var persistenceManager = new NorthwindCorePersistenceManager(dbContext);
        var metadata = persistenceManager.Metadata();
        Console.Out.WriteLine(metadata);
    }
    else
    {
        // Start web server
        CreateWebHostBuilder(args).Build().Run();
    }
}
```
## Testing the metadata generation

Compile the solution, then open a command prompt in the `NorthwindCore/NorthwindServer` directory.  Then run:

`dotnet NorthwindServer\bin\Debug\netcoreapp2.2\NorthwindServer.dll metadata`

This should produce a big blob of JSON that represents the type information that EF knows about our data model.:
```
{"structuralTypes":[{"shortName":"Customer","namespace":"NorthwindModel.Models","autoGeneratedKeyType":"Identity","defaultResourceName":"Customer",...
```

## Testing the server

Compile and run the solution.  It should open a browser and attempt to open a page on the default port, e.g https://localhost:44367 ,
but it may return a 404 error because we haven't enabled static pages.  That's okay; let's test a Breeze query.

Change the URL to https://localhost:{port}/api/breeze/customers

Now you should get a JSON result containing all the rows from the Customers table in the NorthwindCore database:
```
[
  {
    "$id": "1",
    "$type": "NorthwindModel.Models.Customer, NorthwindModel",
    "Id": 1,
    "FirstName": "Maria",
    "LastName": "Anders",
    "City": "Berlin",
    "Country": "Germany",
    "Phone": "030-0074321",
    "Orders": []
  },
  {
    "$id": "2",
    "$type": "NorthwindModel.Models.Customer, NorthwindModel",
    "Id": 2,
    "FirstName": "Ana",
    "LastName": "Trujillo",
    "City": "M\u00e9xico D.F.",
    "Country": "Mexico",
    "Phone": "(5) 555-4729",
    "Orders": []
  }...
]
```
If you don't get the result above, review the previous steps and try to see where you may have gone wrong.

Note that each Customer entity has an `$id` and `$type` property.  

- The `$id` is for resolving circular references - each entity appear only once in the JSON tree, and if the entity is referenced again in the tree, it will be replaced by a `$ref` that references the `$id`.

- The `$type` identifies the entity type.  The Breeze client will use this to determine what type of entity to create on the client as a result of the query.

If you don't see the `$id` and `$type`, go back to the `Startup.cs` file and make sure the settings are correct.

If everything looks good, we are ready to work on the client side.

# Create the Angular App

To create the initial shell of the Angular app, we will use the Angular CLI as instructed in the [Angular.io Guide](https://angular.io/guide/setup-local).

First, make sure you have [nodejs](https://nodejs.org) and [npm](https://docs.npmjs.com/) installed.

Next, open a command prompt in the `NorthwindCore` directory, and follow the steps below:

1. `npm install -g @angular/cli`
2. `ng new NorthwindClient`
  - Would you like to add Angular routing? **Y**
  - Which stylesheet format would you like to use? **CSS**

Now you should have a `NorthwindCore/NorthwindClient` directory containing the Angular app.  Try it out:

`cd NorthwindClient`
`ng serve --open`

This will compile the app and open a browser on http://localhost:4200 with a welcome page.  

Stop the server from the command line using Ctrl-C.

## Add Breeze packages

Now we'll add Breeze to the app, so we can query entities from the server and update them.

Start by adding the npm packages.  In the `NorthwindClient` directory, run:

`npm install breeze-client@next`
`npm install breeze-bridge2-angular`
`npm install breeze-entity-generator`

## Generate Entities

When developing our app, it's helpful to have TypeScript classes to represent the entity data that comes from the server.  

We'll need to generate metadata from our NorthwindServer project, then use that metadata to generate the TypeScript classes.

#### Generate metadata from server

Start by opening a command prompt in the `NorthwindCore\NorthwindServer` directory.  Then run:

`dotnet NorthwindServer\bin\Debug\netcoreapp2.2\NorthwindServer.dll metadata > metadata.json`

That will create a `metadata.json` file in the `NorthwindServer` directory.

#### Generate entities from metadata
To turn the metadata into entities, we need to write a script.  In the `NorthwindClient` directory,
create a file called `generate-entities.js`.

Fill `generate-entities.js` with the following:
```
var tsGen = require('breeze-entity-generator/tsgen-core');
var fs = require('fs');
var dir = './src/app/model';

if (!fs.existsSync(dir)){
    fs.mkdirSync(dir);
}

tsGen.generate({
  inputFileName: '../NorthwindServer/metadata.json',
  outputFolder: dir,
  camelCase: true,
  kebabCaseFileNames: true,
  codePrefix: 'Northwind'
});
```
Then run the file with

`node generate-entities.js`

This should create files in the `NorthwindClient/src/app/model` directory:
```
customer.ts
entity-model.ts
metadata.ts
order-item.ts
order.ts
product.ts
registration-helper.ts
supplier.ts
```
These are the entity classes, plus the metadata and the registration-helper that we will use later.

_Note that you can customize the entity output by changing the parameters to the `generate` function, 
and by changing the template files.  See `node_modules/breeze-entity-generator/README.md` for more information._

## Configure App Module

Now we need to register the Breeze adapters to work with Angular.  

Edit `NorthwindClient\src\app\app.module.ts`.  At the top of the file, add the following imports:
```
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { config, NamingConvention } from 'breeze-client';
import { DataServiceWebApiAdapter } from 'breeze-client/adapter-data-service-webapi';
import { ModelLibraryBackingStoreAdapter } from 'breeze-client/adapter-model-library-backing-store';
import { UriBuilderJsonAdapter } from 'breeze-client/adapter-uri-builder-json';
import { AjaxHttpClientAdapter } from 'breeze-bridge2-angular';
```
Add `HttpClientModule` to the `imports` section of the `@NgModule` declaration:
```
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule
  ],
```

In the class declaration at the bottom of the file, add the constructor:
```
export class AppModule {
  constructor(http: HttpClient) {
    // Configure Breeze adapters
    ModelLibraryBackingStoreAdapter.register();
    UriBuilderJsonAdapter.register();
    config.registerAdapter('ajax', function() { return new AjaxHttpClientAdapter(http); } as any);
    config.initializeAdapterInstance('ajax', AjaxHttpClientAdapter.adapterName, true);
    DataServiceWebApiAdapter.register();
    NamingConvention.camelCase.setAsDefault();
    config.initializeAdapterInstance('uriBuilder', 'json');
  }
}
```
That's a lot of adapters!  Let's look at what they do:
 - ModelLibraryBackingStoreAdapter stores data in entities in a way that is compatible with Angular
 - UriBuilderJsonAdapter encodes Breeze queries in JSON format in query URIs
 - AjaxHttpClientAdapter uses Angular's HttpClient for performing AJAX requests
 - DataServiceWebApiAdapter handles responses from WebAPI and similar backends
 - NamingConvention sets how Breeze converts entity property names between client and server

## Create the environment settings

The client application needs to know the URL to reach the server.  This sort of this is environment-specific,
so we'll keep it in the `environment.ts` file.

Edit `src/environments/environment.ts` and add a line for breezeApiRoot.  The port should be the one
on which your NorthwindServer is listening, and the path is the path to your BreezeController.
```
export const environment = {
  production: false,
  breezeApiRoot: 'https://localhost:44367/api/breeze'
};
```

## Create the EntityManagerProvider

In a Breeze application, the [EntityManager](http://breeze.github.io/doc-js/entitymanager-and-caching.html) caches the
entities and keeps track of the changes.  You may need more than one, if you want to keep multiple change sets.  To make
it easy, we will create a service called the EntityManagerProvider.

_In a real project, now would be a good time to start creating submodules.  For this demo, we will keep everything in one
module for simplicity._

Create the file `NorthwindClient/src/app/entity-manager-provider.ts`.  In the file, put:
```
import { Injectable } from '@angular/core';
import { DataService, EntityManager } from 'breeze-client';
import { environment } from '../environments/environment';
import { NorthwindMetadata } from './model/metadata';
import { NorthwindRegistrationHelper } from './model/registration-helper';

@Injectable({providedIn: 'root'})
export class EntityManagerProvider {

  protected masterManager: EntityManager;

  constructor() {
    const dataService = new DataService({
      serviceName: environment.breezeApiRoot,
      hasServerMetadata: false
    });

    this.masterManager = new EntityManager({ dataService });
    const metadataStore = this.masterManager.metadataStore;
    metadataStore.importMetadata(NorthwindMetadata.value);
    NorthwindRegistrationHelper.register(metadataStore);
  }

  newManager(): EntityManager {
    return this.masterManager.createEmptyCopy();
  }
}
```
This code creates a new "master" manager and configures its metadata.  
It exposes a `newManager()` method that creates a copy of the master complete with metadata.
When we need an EntityManager, we call the `newManager()` method.

## Create the Customer component

Now create a component to display some customer data.  Open a command prompt in the `NorthwindClient` directory, and execute the command:

`ng generate component customer`

Then look in the `src/app/customer` directory to see the component files:
```
customer.component.css
customer.component.html
customer.component.spec.ts
customer.component.ts
```
## Show the Customer component

The new component won't display yet because there is no way to get to it.  We will fix that by changing the app module to route to it.

First, edit `app.component.html` and delete **everything except** the `router-outlet` tag.  Then add a heading:
```
<h1>Northwind</h1>
<router-outlet></router-outlet>
```
Then edit `app-routing.module.ts` and add some routes:
```
const routes: Routes = [
  {
    path: 'customers',
    component: CustomerComponent,
  },
  {
    path      : '**',
    redirectTo: 'customers'
  }
];
```
The first route will load the CustomerComponent into the `router-outlet` if the url is /customers.

The second route will redirect to /customers if the current url doesn't match any other route.  Since there are no other routes, our CustomerComponent should always be displayed.

Try it now: if the app is not already running, open a command prompt in the `NorthwindClient` directory and run:

`ng serve --open`

You should see a screen that says "Northwind" followed by "customer works!".

### Start the server

Start the `NorthwindServer` project now, so it will be available to serve data requests.

### Get Customer data

Edit `customer.component.ts`.  In the body of the `CustomerComponent` class, add fields for EntityManager and a list of Customers, and change the constructor so it accepts an EntityManagerProvider and stores it in a private field:
```
  manager: EntityManager;
  customers: Customer[];
  constructor(private entityManagerProvider: EntityManagerProvider) { }
```
Then edit the `ngOnInit` method to create an EntityManager and perform a Breeze query:
```
  ngOnInit() {
    this.manager = this.entityManagerProvider.newManager();
    const query = new EntityQuery('Customers').where('lastName', 'startsWith', 'C');
    this.manager.executeQuery(query).then(qr => {
      this.customers = qr.results;
    });
  }
```
The query results are assigned to the `customers` field in the component.

### Show the data

Edit the HTML template, `customer.component.html`.  Add a `ngFor` loop to display some properties of the customers:
```
<p>customer works!</p>

<div *ngFor="let cust of customers">
  {{cust.firstName}} {{cust.lastName}}
</div>
```
Now you should see the data display on the page:
```
Northwind

customer works!

Frédérique Citeaux
Francisco Chang
Aria Cruz
Philip Cramer
Simon Crowther
Lúcia Carvalho
Alejandra Camino
Pascale Cartrain
```





