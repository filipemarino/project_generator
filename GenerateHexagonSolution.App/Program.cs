using System.Diagnostics;
using System.IO;
using System;
using McMaster.Extensions.CommandLineUtils;
using System.Collections.Generic;
using System.Text;

namespace GenerateHexagonSolution.App
{
    [Command(Name = "Generate Solution App", Description = "This app generates solution templates of webapi projects based on the hexagon architecture")]
    [HelpOption("--help")]
    public class Program
    {
        static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        [Option("-n|--name", Description = "Sets the solution name")]
        private string SolutionName { get; set; }
        
        [Option("-d|--directory", Description = "Sets the solution directory")]
        private string DirectoryName { get; set; }

        [Option("-f|--framework", Description = "Sets the framework for the solution (default netcoreapp3.1)")]
        private string Framework { get; set; }

        private string _solutionPath = "";

        private void OnExecute()
        {
            if (string.IsNullOrEmpty(SolutionName) || string.IsNullOrWhiteSpace(SolutionName))
            {
                Console.WriteLine("The solution name is required. See --help for available commands");
                Console.WriteLine();
                return;
            }

            if (string.IsNullOrEmpty(DirectoryName) || string.IsNullOrWhiteSpace(DirectoryName))
            {
                Console.WriteLine("The working directory is required. See --help for available commands");
                Console.WriteLine();
                return;
            }

            if (string.IsNullOrEmpty(Framework) || string.IsNullOrWhiteSpace(Framework))
                Framework = "netcoreapp3.1";

            _solutionPath = $@"{DirectoryName}\{SolutionName}";

            CreateSolutionDirectory();

            //CORE
            List<string> packageListBasic = new List<string>();
            packageListBasic.Add("AutoMapper");
            packageListBasic.Add("Microsoft.Extensions.DependencyInjection.Abstractions");
            packageListBasic.Add("Microsoft.Extensions.Logging.Abstractions");

            CreateProject("Domain", packageListBasic);
            CreateProject("Helpers", packageListBasic);

            //INFRA
            List<string> packageListDapper = new List<string>();
            packageListDapper.Add("AutoMapper");
            packageListDapper.Add("Dapper");
            packageListDapper.Add("Dapper.FluentMap");
            packageListDapper.Add("Dapper.FluentMap.Dommel");
            packageListDapper.Add("Microsoft.Extensions.DependencyInjection.Abstractions");
            packageListDapper.Add("Microsoft.Extensions.Logging.Abstractions");
            packageListDapper.Add("Microsoft.Extensions.Configuration");
            packageListDapper.Add("System.Data.SqlClient");

            List<string> packageListExternalServices = new List<string>();
            packageListExternalServices.Add("AutoMapper");
            packageListExternalServices.Add("Microsoft.Extensions.DependencyInjection.Abstractions");
            packageListExternalServices.Add("Microsoft.Extensions.Logging.Abstractions");
            packageListExternalServices.Add("Microsoft.Extensions.Http");
            
            CreateProject("Dapper", packageListDapper);
            CreateProject("ExternalServices", packageListExternalServices);

            //APPLICATION
            CreateProject("Application", packageListBasic);

            //CROSSCUTTING
            List<string> packageListIoC = new List<string>();
            packageListIoC.Add("AutoMapper.Extensions.Microsoft.DependencyInjection");
            packageListIoC.Add("Microsoft.Extensions.Http");
            packageListIoC.Add("Serilog.Enrichers.Environment");
            packageListIoC.Add("Serilog.Extensions.Logging");
            packageListIoC.Add("Serilog.Sinks.MSSqlServer");
            packageListIoC.Add("Swashbuckle.AspNetCore.SwaggerGen");
            packageListIoC.Add("Dapper");
            packageListIoC.Add("Dapper.FluentMap");
            packageListIoC.Add("Dapper.FluentMap.Dommel");
            packageListIoC.Add("Microsoft.Extensions.Configuration");
            
            CreateProject("IoC", packageListIoC);

            //PRESENTATION
            List<string> packageListWebAPI = new List<string>();
            packageListWebAPI.Add("Microsoft.AspNetCore.Mvc.Versioning");
            packageListWebAPI.Add("Microsoft.Extensions.DependencyInjection.Abstractions");
            packageListWebAPI.Add("Microsoft.VisualStudio.Web.CodeGeneration.Design");
            packageListWebAPI.Add("Swashbuckle.AspNetCore.Swagger");
            packageListWebAPI.Add("Swashbuckle.AspNetCore.SwaggerGen");
            packageListWebAPI.Add("Swashbuckle.AspNetCore.SwaggerUI");

            CreateProject("WebAPI", packageListWebAPI);

            //TESTS
            List<string> packageListTests = new List<string>();
            packageListTests.Add("AutoFixture");
            packageListTests.Add("AutoMapper");
            packageListTests.Add("MockQueryable.Core");
            packageListTests.Add("MockQueryable.Moq");
            packageListTests.Add("Moq");
            packageListTests.Add("Serilog.Enrichers.Environment");
            packageListTests.Add("Serilog.Sinks.MSSqlServer");
            packageListTests.Add("ServiceStack.OrmLite.Sqlite");
            packageListTests.Add("SQLite");

            CreateProject("Tests", packageListTests);

            AddBootstraperClass();
            AddStartupClass();
            AddControllerExample();
            AddLaunchJson();
            AddDatabaseEnum();
            AddModel();
            AddDapperMap();
            AddDapperConnectionFactory();
            AddDapperInterfaces();
            AddDapperRepositories();
            AddHTTPCommClass();
        }

        private void CreateSolutionDirectory()
        {
            Console.WriteLine($"Create solution file {SolutionName}.sln in the directory {_solutionPath}");

            if (!Directory.Exists(_solutionPath))
                Directory.CreateDirectory(_solutionPath);

            Process.Start(@"cmd", $"/c dotnet new sln -n \"{SolutionName}\" -o \"{_solutionPath}\"").WaitForExit();

            Console.WriteLine();
        }

        private void CreateProject(string projectName, List<string> nugetPackages)
        {
            Console.WriteLine($"Create project {SolutionName}.{projectName}");

            string projectPath = @$"{_solutionPath}\{SolutionName}.{projectName}";

            if (!Directory.Exists(projectPath))
                Directory.CreateDirectory(projectPath);

            if (projectName == "WebAPI")
                Process.Start(@"cmd", $"/c dotnet new webapi -n \"{SolutionName}.{projectName}\" -o \"{projectPath}\" -f {Framework}").WaitForExit();
            else if (projectName == "Tests")
                Process.Start(@"cmd", $"/c dotnet new xunit -n \"{SolutionName}.{projectName}\" -o \"{projectPath}\" -f {Framework}").WaitForExit();
            else
                Process.Start(@"cmd", $"/c dotnet new classlib -n \"{SolutionName}.{projectName}\" -o \"{projectPath}\" -f {Framework}").WaitForExit();
            
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = @"cmd";
            psi.WorkingDirectory = $"{_solutionPath}";
            psi.Arguments = @$"/c dotnet sln {SolutionName}.sln add {_solutionPath}\{SolutionName}.{projectName}\{SolutionName}.{projectName}.csproj";
            Process.Start(psi).WaitForExit();

            Console.WriteLine();

            AddPackagesNuget($"{SolutionName}.{projectName}", nugetPackages);

            if (projectName != "Domain" && projectName != "Helpers")
            {
                switch (projectName)
                {
                    case "Dapper":
                    case "ExternalServices":
                        AddReferences("INFRA", $"{SolutionName}.{projectName}");
                        break;
                    case "IoC":
                        AddReferences("CROSSCUTTING", $"{SolutionName}.{projectName}");
                        break;
                    case "Application":
                        AddReferences("APPLICATION", $"{SolutionName}.{projectName}");
                        break;
                    case "WebAPI":
                        AddReferences("PRESENTATION", $"{SolutionName}.{projectName}");
                        break;
                    default:
                        AddReferences("TESTS", $"{SolutionName}.{projectName}");
                        break;
                }
            }
        }
    
        private void AddReferences(string layer, string project)
        {
            if (layer == "INFRA")
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Domain\{SolutionName}.Domain.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Helpers\{SolutionName}.Helpers.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
            }
            else if (layer == "APPLICATION")
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Domain\{SolutionName}.Domain.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Helpers\{SolutionName}.Helpers.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Dapper\{SolutionName}.Dapper.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
                
                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.ExternalServices\{SolutionName}.ExternalServices.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
            }
            else if (layer == "PRESENTATION")
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Domain\{SolutionName}.Domain.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
                
                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Helpers\{SolutionName}.Helpers.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.IoC\{SolutionName}.IoC.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
            }
            else if (layer == "CROSSCUTTING")
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Domain\{SolutionName}.Domain.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
                
                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Application\{SolutionName}.Application.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Dapper\{SolutionName}.Dapper.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.ExternalServices\{SolutionName}.ExternalServices.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Helpers\{SolutionName}.Helpers.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
            }
            else
            {
                //TESTS
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Domain\{SolutionName}.Domain.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
                
                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Application\{SolutionName}.Application.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Dapper\{SolutionName}.Dapper.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.ExternalServices\{SolutionName}.ExternalServices.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.IoC\{SolutionName}.IoC.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.Helpers\{SolutionName}.Helpers.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();

                psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = @$"/c dotnet add reference ..\{SolutionName}.WebAPI\{SolutionName}.WebAPI.csproj";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
            }
        }
    
        private void AddPackagesNuget(string project, List<string> packageList)
        {
            Console.WriteLine($"Add nuget packages for project {project}");

            foreach(var package in packageList)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.WorkingDirectory = @$"{_solutionPath}\{project}";
                psi.Arguments = $"/c dotnet add package {package}";
                Process.Start(psi).WaitForExit();
                Console.WriteLine();
            }
        }
    
        private void AddBootstraperClass()
        {
            Console.WriteLine("Start creating file Bootstraper.cs");

            string filePath = @$"{_solutionPath}\{SolutionName}.IoC\Bootstraper.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assembly Bootstraper.cs file

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using Microsoft.OpenApi.Models;");
            sb.AppendLine("using Serilog;");
            sb.AppendLine("using Dapper.FluentMap;");
            sb.AppendLine("using Dapper.FluentMap.Dommel;");
            sb.AppendLine("using Serilog.Events;");
            sb.AppendLine("using Serilog.Extensions.Logging;");
            sb.AppendLine("using Serilog.Filters;");
            sb.AppendLine("using Serilog.Sinks.MSSqlServer;");
            sb.AppendLine("using Swashbuckle.AspNetCore.SwaggerGen;");
            sb.AppendLine($"using {SolutionName}.Domain.Enums;");
            sb.AppendLine($"using {SolutionName}.Dapper.Maps;");
            sb.AppendLine($"using {SolutionName}.Dapper.Factory;");
            sb.AppendLine($"using {SolutionName}.Dapper.Repositories;");
            sb.AppendLine($"using {SolutionName}.Domain.Interfaces.Repositories;");
            sb.AppendLine($"using {SolutionName}.Domain.Interfaces.Repositories.ExternalServices;");
            sb.AppendLine($"using {SolutionName}.ExternalServices.Base;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.IoC");
            sb.AppendLine("{");
            sb.AppendLine("    public class Bootstraper");
            sb.AppendLine("    {");
            sb.AppendLine("        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)");
            sb.AppendLine("        {");
            sb.AppendLine("            //Logger");
            sb.AppendLine("            var providers = new LoggerProviderCollection();");
            sb.AppendLine("");
            sb.AppendLine("            var connectionString = configuration.GetSection(\"ConnectionStrings:Default:DataSource\").Value;");
            sb.AppendLine("");
            sb.AppendLine("            Log.Logger = new LoggerConfiguration()");
            sb.AppendLine("                .MinimumLevel.Warning()");
            sb.AppendLine("                .WriteTo.Providers(providers)");
            sb.AppendLine("                    .MinimumLevel.Warning()");
            sb.AppendLine("                    .MinimumLevel.Override(\"System\", LogEventLevel.Warning)");
            sb.AppendLine("                    .MinimumLevel.Override(\"Microsoft\", LogEventLevel.Warning)");
            sb.AppendLine("                    .Enrich.WithProperty(\"App Name\", \"TEMPLATE PROJECT\")");
            sb.AppendLine("                    .Enrich.FromLogContext()");
            sb.AppendLine("                    .Enrich.WithMachineName()");
            sb.AppendLine("                    .Enrich.WithEnvironmentUserName()");
            sb.AppendLine("                    .Filter.ByExcluding(Matching.FromSource(\"Microsoft.AspNetCore.StaticFiles\"))");
            sb.AppendLine("                    .WriteTo.MSSqlServer(");
            sb.AppendLine("                        connectionString: connectionString,");
            sb.AppendLine("                        sinkOptions: new MSSqlServerSinkOptions");
            sb.AppendLine("                        {");
            sb.AppendLine("                            TableName = \"NOME_TABELA_LOG\",");
            sb.AppendLine("                            AutoCreateSqlTable = true,");
            sb.AppendLine("                            SchemaName = \"dbo\"");
            sb.AppendLine("                        },");
            sb.AppendLine("                        restrictedToMinimumLevel: LogEventLevel.Warning,");
            sb.AppendLine("                        columnOptions: GetSqlColumnOptions()");
            sb.AppendLine("                    )");
            sb.AppendLine("                .CreateLogger();");
            sb.AppendLine("");
            sb.AppendLine("            var connectionDict = new Dictionary<EnumDatabaseConnection, string>");
            sb.AppendLine("            {");
            sb.AppendLine("                { EnumDatabaseConnection.DBDefault, configuration.GetSection(\"ConnectionStrings:Default:DataSource\").Value}");
            sb.AppendLine("            };");
            sb.AppendLine("");
            sb.AppendLine("            services.AddSingleton<IDictionary<EnumDatabaseConnection, string>>(connectionDict);");
            sb.AppendLine("            services.AddTransient<IDbConnectionFactory, DbConnectionFactory>();");
            sb.AppendLine("");
            sb.AppendLine("            FluentMapper.Initialize(config => ");
            sb.AppendLine("            {");
            sb.AppendLine("                config.AddMap(new ClientMap(configuration));");
            sb.AppendLine("                config.ForDommel();");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            services.AddScoped<IUnitOfWork, UnitOfWork>();");
            sb.AppendLine("            services.AddScoped<IClientRepository, ClientRepository>();");
            sb.AppendLine("            services.AddScoped<IHTTP, HTTP>();");
            sb.AppendLine("            services.AddHttpClient();");
            sb.AppendLine("");
            sb.AppendLine("            services.AddSwaggerGen(x => ");
            sb.AppendLine("            {");
            sb.AppendLine("                x.SwaggerDoc(\"v1\", new OpenApiInfo");
            sb.AppendLine("                {");
            sb.AppendLine("                    Title = \"Projeto Template WebAPI\",");
            sb.AppendLine("                    Version = \"V1\",");
            sb.AppendLine("                    Description = \"LOREN IPSUN TEMPLATE WHATEVER\",");
            sb.AppendLine("                    //TermsOfService = new Uri(\"some http\")");
            sb.AppendLine("                    Contact = new OpenApiContact");
            sb.AppendLine("                    {");
            sb.AppendLine("                        Name = \"Contate o desenvolvedor\",");
            sb.AppendLine("                        Email = \"something@somewhere.com\"");
            sb.AppendLine("                    },");
            sb.AppendLine("                    License = new OpenApiLicense");
            sb.AppendLine("                    {");
            sb.AppendLine("                        Name = \"Apache 2.0\",");
            sb.AppendLine("                        Url = new Uri(\"http://www.apache.org/licences/LICENCE-2.0.html\")");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("");
            sb.AppendLine("                x.DocInclusionPredicate((docName, apiDescription) => ");
            sb.AppendLine("                {");
            sb.AppendLine("                    var actionDescriptor = apiDescription.ActionDescriptor.DisplayName;");
            sb.AppendLine("");
            sb.AppendLine("                    if (actionDescriptor != null)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        if (actionDescriptor.Contains(docName))");
            sb.AppendLine("                        {");
            sb.AppendLine("                            var values = apiDescription.RelativePath.Split('/').Select(v => v.Replace(\"v{version}\", docName));");
            sb.AppendLine("");
            sb.AppendLine("                            apiDescription.RelativePath = string.Join(\"/\", values);");
            sb.AppendLine("");
            sb.AppendLine("                            return true;");
            sb.AppendLine("                        }");
            sb.AppendLine("                        else");
            sb.AppendLine("                            return false;");
            sb.AppendLine("                    }");
            sb.AppendLine("");
            sb.AppendLine("                    return false;");
            sb.AppendLine("                });");
            sb.AppendLine("");
            sb.AppendLine("                x.OperationFilter<RemoveVersionFromParameter>();");
            sb.AppendLine("                x.DocumentFilter<ReplaceVersionWithExactValueInPath>();");
            sb.AppendLine("");
            sb.AppendLine("                x.AddSecurityDefinition(\"Bearer\", new OpenApiSecurityScheme");
            sb.AppendLine("                {");
            sb.AppendLine("                    In = ParameterLocation.Header,");
            sb.AppendLine("                    Description = \"Please, insert JWT token at the header with 'Bearer ' at the start\",");
            sb.AppendLine("                    Name = \"Authorization\",");
            sb.AppendLine("                    Type = SecuritySchemeType.ApiKey");
            sb.AppendLine("                });");
            sb.AppendLine("");
            sb.AppendLine("                x.AddSecurityRequirement(new OpenApiSecurityRequirement");
            sb.AppendLine("                {");
            sb.AppendLine("                    {");
            sb.AppendLine("                        new OpenApiSecurityScheme");
            sb.AppendLine("                        {");
            sb.AppendLine("                            Reference = new OpenApiReference");
            sb.AppendLine("                            {");
            sb.AppendLine("                                Type = ReferenceType.SecurityScheme,");
            sb.AppendLine("                                Id = \"Bearer\"");
            sb.AppendLine("                            }");
            sb.AppendLine("                        },");
            sb.AppendLine("                        new string[] { }");
            sb.AppendLine("                    }");
            sb.AppendLine("                });");
            sb.AppendLine("");
            sb.AppendLine("                var xmlFile = $\"{System.Reflection.Assembly.GetEntryAssembly().GetName().Name}.XML\";");
            sb.AppendLine("                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);");
            sb.AppendLine("");
            sb.AppendLine("                x.IncludeXmlComments(xmlPath);");
            sb.AppendLine("            });");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public static ColumnOptions GetSqlColumnOptions()");
            sb.AppendLine("        {");
            sb.AppendLine("            var colOptions = new ColumnOptions();");
            sb.AppendLine("            colOptions.DisableTriggers = true;");
            sb.AppendLine("            colOptions.ClusteredColumnstoreIndex = false;");
            sb.AppendLine("            colOptions.Store.Add(StandardColumn.LogEvent);");
            sb.AppendLine("");
            sb.AppendLine("            colOptions.Store.Remove(StandardColumn.Properties);");
            sb.AppendLine("            colOptions.Store.Remove(StandardColumn.MessageTemplate);");
            sb.AppendLine("");
            sb.AppendLine("            colOptions.PrimaryKey = colOptions.Id;");
            sb.AppendLine("            colOptions.Id.NonClusteredIndex = true;");
            sb.AppendLine("");
            sb.AppendLine("            colOptions.Level.ColumnName = \"Severity\";");
            sb.AppendLine("            colOptions.Level.StoreAsEnum = false;");
            sb.AppendLine("            ");
            sb.AppendLine("            colOptions.Properties.ColumnName = \"Properties\";");
            sb.AppendLine("            colOptions.Properties.ExcludeAdditionalProperties = true;");
            sb.AppendLine("            colOptions.Properties.DictionaryElementName = \"dict\";");
            sb.AppendLine("            colOptions.Properties.ItemElementName = \"item\";");
            sb.AppendLine("            colOptions.Properties.OmitSequenceContainerElement = false;");
            sb.AppendLine("            colOptions.Properties.OmitStructureContainerElement = false;");
            sb.AppendLine("            colOptions.Properties.OmitElementIfEmpty = true;");
            sb.AppendLine("            colOptions.Properties.PropertyElementName = \"prop\";");
            sb.AppendLine("            colOptions.Properties.SequenceElementName = \"seq\";");
            sb.AppendLine("            colOptions.Properties.StructureElementName = \"struct\";");
            sb.AppendLine("            colOptions.Properties.UsePropertyKeyAsElementName = false;");
            sb.AppendLine("            ");
            sb.AppendLine("            colOptions.TimeStamp.ColumnName = \"DtInc\";");
            sb.AppendLine("");
            sb.AppendLine("            colOptions.LogEvent.ExcludeAdditionalProperties = true;");
            sb.AppendLine("            colOptions.LogEvent.ExcludeStandardColumns = true;");
            sb.AppendLine("");
            sb.AppendLine("            colOptions.Message.ColumnName = \"Msg\";");
            sb.AppendLine("");
            sb.AppendLine("            return colOptions;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("");
            sb.AppendLine("    public class RemoveVersionFromParameter : IOperationFilter");
            sb.AppendLine("    {");
            sb.AppendLine("        public void Apply(OpenApiOperation operation, OperationFilterContext context)");
            sb.AppendLine("        {");
            sb.AppendLine("            var versionParameter = operation.Parameters.Single(p => p.Name == \"version\");");
            sb.AppendLine("            operation.Parameters.Remove(versionParameter);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("");
            sb.AppendLine("    public class ReplaceVersionWithExactValueInPath : IDocumentFilter");
            sb.AppendLine("    {");
            sb.AppendLine("        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)");
            sb.AppendLine("        {");
            sb.AppendLine("            var paths = new OpenApiPaths();");
            sb.AppendLine("            foreach(var path in swaggerDoc.Paths)");
            sb.AppendLine("            {");
            sb.AppendLine("                paths.Add(path.Key.Replace(\"v{version}\", swaggerDoc.Info.Version), path.Value);");
            sb.AppendLine("            }");
            sb.AppendLine("");
            sb.AppendLine("            swaggerDoc.Paths = paths;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }

        private void AddStartupClass()
        {
            Console.WriteLine("Start creating file Startup.cs");

            string filePath = @$"{_solutionPath}\{SolutionName}.WebAPI\Startup.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assembly Startup.cs file

            sb.AppendLine("using Microsoft.AspNetCore.Builder;");
            sb.AppendLine("using Microsoft.AspNetCore.Hosting;");
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using Microsoft.Extensions.Hosting;");
            sb.AppendLine($"using {SolutionName}.IoC;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.WebAPI");
            sb.AppendLine("{");
            sb.AppendLine("    public class Startup");
            sb.AppendLine("    {");
            sb.AppendLine("        public Startup(IConfiguration configuration)");
            sb.AppendLine("        {");
            sb.AppendLine("            Configuration = configuration;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public IConfiguration Configuration { get; }");
            sb.AppendLine("");
            sb.AppendLine("        // This method gets called by the runtime. Use this method to add services to the container.");
            sb.AppendLine("        public void ConfigureServices(IServiceCollection services)");
            sb.AppendLine("        {");
            sb.AppendLine("            services.AddControllers();");
            sb.AppendLine("            services.AddApiVersioning(p => ");
            sb.AppendLine("            {");
            sb.AppendLine("                p.DefaultApiVersion = new ApiVersion(1, 0);");
            sb.AppendLine("                p.ReportApiVersions = true;");
            sb.AppendLine("                p.AssumeDefaultVersionWhenUnspecified = true;");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            Bootstraper bootstraper = new Bootstraper();");
            sb.AppendLine("            bootstraper.ConfigureServices(services, Configuration);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.");
            sb.AppendLine("        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (env.IsDevelopment())");
            sb.AppendLine("            {");
            sb.AppendLine("                app.UseDeveloperExceptionPage();");
            sb.AppendLine("            }");
            sb.AppendLine("");
            sb.AppendLine("            app.UseHttpsRedirection();");
            sb.AppendLine("");
            sb.AppendLine("            app.UseRouting();");
            sb.AppendLine("");
            sb.AppendLine("            app.UseAuthorization();");
            sb.AppendLine("");
            sb.AppendLine("            app.UseEndpoints(endpoints =>");
            sb.AppendLine("            {");
            sb.AppendLine("                endpoints.MapControllers();");
            sb.AppendLine("            });");
            sb.AppendLine("");
            sb.AppendLine("            app.UseSwagger();");
            sb.AppendLine("            app.UseSwaggerUI(c => ");
            sb.AppendLine("            {");
            sb.AppendLine("                c.SwaggerEndpoint(\"/swagger/v1/swagger.json\", \"V1\");");
            sb.AppendLine("            });");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }

        private void AddControllerExample()
        {
            Console.WriteLine("Start creating example controller file");

            File.Delete(@$"{_solutionPath}\{SolutionName}.WebAPI\Controllers\WeatherForecastController.cs");

            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.WebAPI\Controllers\v1");

            string filePath = @$"{_solutionPath}\{SolutionName}.WebAPI\Controllers\v1\WeatherForecastController.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assembly example controller file

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Microsoft.Extensions.Logging;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.WebAPI.Controllers.v1");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Class");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [ApiController]");
            sb.AppendLine("    [Route(\"api/v{version:apiVersion}/[controller]\")]");
            sb.AppendLine("    [ApiVersion(\"1\")]");
            sb.AppendLine("    public class WeatherForecastController : ControllerBase");
            sb.AppendLine("    {");
            sb.AppendLine("        private static readonly string[] Summaries = new[]");
            sb.AppendLine("        {");
            sb.AppendLine("            \"Freezing\", \"Bracing\", \"Chilly\", \"Cool\", \"Mild\", \"Warm\", \"Balmy\", \"Hot\", \"Sweltering\", \"Scorching\"");
            sb.AppendLine("        };");
            sb.AppendLine("");
            sb.AppendLine("        private readonly ILogger<WeatherForecastController> _logger;");
            sb.AppendLine("");
            sb.AppendLine("        public WeatherForecastController(ILogger<WeatherForecastController> logger)");
            sb.AppendLine("        {");
            sb.AppendLine("            _logger = logger;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Method 1");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <returns></returns>");
            sb.AppendLine("        [HttpGet]");
            sb.AppendLine("        [MapToApiVersion(\"1\")]");
            sb.AppendLine("        public IEnumerable<WeatherForecast> Get()");
            sb.AppendLine("        {");
            sb.AppendLine("            var rng = new Random();");
            sb.AppendLine("            return Enumerable.Range(1, 5).Select(index => new WeatherForecast");
            sb.AppendLine("            {");
            sb.AppendLine("                Date = DateTime.Now.AddDays(index),");
            sb.AppendLine("                TemperatureC = rng.Next(-20, 55),");
            sb.AppendLine("                Summary = Summaries[rng.Next(Summaries.Length)]");
            sb.AppendLine("            })");
            sb.AppendLine("            .ToArray();");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Method 2");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"id\">Param</param>");
            sb.AppendLine("        /// <returns></returns>");
            sb.AppendLine("        [HttpGet(\"{id}\")]");
            sb.AppendLine("        [MapToApiVersion(\"1\")]");
            sb.AppendLine("        public WeatherForecast Get(int id)");
            sb.AppendLine("        {");
            sb.AppendLine("            var rng = new Random();");
            sb.AppendLine("            return Enumerable.Range(1, 5).Select(index => new WeatherForecast");
            sb.AppendLine("            {");
            sb.AppendLine("                Date = DateTime.Now.AddDays(index),");
            sb.AppendLine("                TemperatureC = rng.Next(-20, 55),");
            sb.AppendLine("                Summary = Summaries[rng.Next(Summaries.Length)]");
            sb.AppendLine("            })");
            sb.AppendLine("            .ToArray().FirstOrDefault();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");     

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }
    
        private void AddLaunchJson()
        {
            Console.WriteLine("Start creating example launch json file");

            string filePath = @$"{_solutionPath}\{SolutionName}.WebAPI\launchSettings.json";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assembly launch json file

            sb.AppendLine("{");
            sb.AppendLine("  \"$schema\": \"http://json.schemastore.org/launchsettings.json\",");
            sb.AppendLine("  \"iisSettings\": {");
            sb.AppendLine("    \"windowsAuthentication\": false,");
            sb.AppendLine("    \"anonymousAuthentication\": true,");
            sb.AppendLine("    \"iisExpress\": {");
            sb.AppendLine("      \"applicationUrl\": \"http://localhost:38397\",");
            sb.AppendLine("      \"sslPort\": 44362");
            sb.AppendLine("    }");
            sb.AppendLine("  },");
            sb.AppendLine("  \"profiles\": {");
            sb.AppendLine("    \"IIS Express\": {");
            sb.AppendLine("      \"commandName\": \"IISExpress\",");
            sb.AppendLine("      \"launchBrowser\": true,");
            sb.AppendLine("      \"launchUrl\": \"swagger\",");
            sb.AppendLine("      \"environmentVariables\": {");
            sb.AppendLine("        \"ASPNETCORE_ENVIRONMENT\": \"Development\"");
            sb.AppendLine("      }");
            sb.AppendLine("    },");
            sb.AppendLine("    \"Project.Test.WebAPI\": {");
            sb.AppendLine("      \"commandName\": \"Project\",");
            sb.AppendLine("      \"launchBrowser\": true,");
            sb.AppendLine("      \"launchUrl\": \"swagger\",");
            sb.AppendLine("      \"applicationUrl\": \"https://localhost:5001;http://localhost:5000\",");
            sb.AppendLine("      \"environmentVariables\": {");
            sb.AppendLine("        \"ASPNETCORE_ENVIRONMENT\": \"Development\"");
            sb.AppendLine("      }");
            sb.AppendLine("    }");
            sb.AppendLine("  }");
            sb.AppendLine("}");       

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }

        private void AddDatabaseEnum()
        {
            Console.WriteLine("Start enum database class");

            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.Domain\Enums");

            string filePath = @$"{_solutionPath}\{SolutionName}.Domain\Enums\EnumDatabaseConnection.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assembly enum databases

            sb.AppendLine($"namespace {SolutionName}.Domain.Enums");
            sb.AppendLine("{");
            sb.AppendLine("    public enum EnumDatabaseConnection");
            sb.AppendLine("    {");
            sb.AppendLine("        DBDefault");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }

        private void AddModel()
        {
            Console.WriteLine("Start model class creation");

            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.Domain\Models");

            string filePath = @$"{_solutionPath}\{SolutionName}.Domain\Models\Client.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assemble model class

            sb.AppendLine("using System;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Domain.Models");
            sb.AppendLine("{");
            sb.AppendLine("    public class Client");
            sb.AppendLine("    {");
            sb.AppendLine("        public long Id { get; set; }");
            sb.AppendLine("        public string Name { get; set; }");
            sb.AppendLine("        public string Address { get; set; }");
            sb.AppendLine("        public DateTime Birthday { get; set; }");
            sb.AppendLine("        public DateTime CreatedDate { get; set; }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }

        private void AddDapperMap()
        {
            Console.WriteLine("Start Dapper map creation");

            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.Dapper\Maps");

            string filePath = @$"{_solutionPath}\{SolutionName}.Dapper\Maps\ClientMap.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assembly model class

            sb.AppendLine("using Dapper.FluentMap.Dommel.Mapping;");
            sb.AppendLine("using Project.Test.Domain.Models;");
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Dapper.Maps");
            sb.AppendLine("{");
            sb.AppendLine("    public class ClientMap : DommelEntityMap<Client>");
            sb.AppendLine("    {");
            sb.AppendLine("        public ClientMap(IConfiguration configuration)");
            sb.AppendLine("        {");
            sb.AppendLine("            var schema = configuration.GetSection(\"ConnectionStrings:Default:Schema\").Value;");
            sb.AppendLine("");
            sb.AppendLine("            if (!string.IsNullOrEmpty(schema))");
            sb.AppendLine("                ToTable(\"TBL_CLIENTE\", configuration.GetSection(\"ConnectionStrings:Default:Schema\").Value);");
            sb.AppendLine("            else");
            sb.AppendLine("                ToTable(\"TBL_CLIENTE\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }
    
        private void AddDapperConnectionFactory()
        {
            Console.WriteLine("Start Dapper factory connection class");

            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.Dapper\Factory");

            string filePath = @$"{_solutionPath}\{SolutionName}.Dapper\Factory\DbConnectionFactory.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assembly Factory class

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Data.Common;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("using System.Data.SqlClient;");
            sb.AppendLine($"using {SolutionName}.Domain.Enums;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Dapper.Factory");
            sb.AppendLine("{");
            sb.AppendLine("    public class DbConnectionFactory : IDbConnectionFactory");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly IDictionary<EnumDatabaseConnection, string> _connectionDict;");
            sb.AppendLine("");
            sb.AppendLine("        public DbConnectionFactory(IDictionary<EnumDatabaseConnection, string> connectionDict)");
            sb.AppendLine("        {");
            sb.AppendLine("            _connectionDict = connectionDict;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public IDbConnection CreateDBConnection(EnumDatabaseConnection database)");
            sb.AppendLine("        {");
            sb.AppendLine("            string connectionString = string.Empty;");
            sb.AppendLine("");
            sb.AppendLine("            if (_connectionDict.TryGetValue(database, out connectionString))");
            sb.AppendLine("            {");
            sb.AppendLine("                DbProviderFactory factory = DbProviderFactories.GetFactory(\"System.Data.SqlClient\");");
            sb.AppendLine("                var conn = factory.CreateConnection();");
            sb.AppendLine("                conn.ConnectionString = connectionString;");
            sb.AppendLine("                return conn;");
            sb.AppendLine("            }");
            sb.AppendLine("");
            sb.AppendLine("            throw new ArgumentNullException();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            filePath = @$"{_solutionPath}\{SolutionName}.Dapper\Factory\IDbConnectionFactory.cs";

            file = File.Create(filePath);
            
            file.Close();
            
            sb = new StringBuilder();
            
            #region Assembly Factory interface 

            sb.AppendLine("using System.Data;");
            sb.AppendLine($"using {SolutionName}.Domain.Enums;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Dapper.Factory");
            sb.AppendLine("{");
            sb.AppendLine("    public interface IDbConnectionFactory");
            sb.AppendLine("    {");
            sb.AppendLine("        IDbConnection CreateDBConnection(EnumDatabaseConnection database);");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion

            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }
    
        private void AddDapperInterfaces()
        {
            Console.WriteLine("Start dapper interfaces / repositories creation");

            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.Domain\Interfaces");
            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.Domain\Interfaces\Repositories");

            string filePath = @$"{_solutionPath}\{SolutionName}.Domain\Interfaces\Repositories\IUnitOfWork.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assemble unit of work interface

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Data;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Domain.Interfaces.Repositories");
            sb.AppendLine("{");
            sb.AppendLine("    public interface IUnitOfWork : IDisposable");
            sb.AppendLine("    {");
            sb.AppendLine("        IDbConnection DbConnection { get; }");
            sb.AppendLine("        IDbTransaction DbTransaction { get; }");
            sb.AppendLine("        void Begin();");
            sb.AppendLine("        void Commit();");
            sb.AppendLine("        void Rollback();");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            filePath = @$"{_solutionPath}\{SolutionName}.Domain\Interfaces\Repositories\ICrudRepository.cs";

            file = File.Create(filePath);
            
            file.Close();
            
            sb = new StringBuilder();
            
            #region Assemble crud interface

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq.Expressions;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Domain.Interfaces.Repositories");
            sb.AppendLine("{");
            sb.AppendLine("    public interface ICrudRepository<T> where T : class");
            sb.AppendLine("    {");
            sb.AppendLine("        Task<IEnumerable<T>> GetAllAsync();");
            sb.AppendLine("        Task<T> GetByIdAsync(object id);");
            sb.AppendLine("        Task<T> SelectFirstAsync(Expression<Func<T, bool>> predicate);");
            sb.AppendLine("        Task<IEnumerable<T>> SelectAllAsync(Expression<Func<T, bool>> predicate);");
            sb.AppendLine("        Task InsertAsync(T obj);");
            sb.AppendLine("        Task UpdateAsync(T obj);");
            sb.AppendLine("        Task DeleteAsync(T obj);");
            sb.AppendLine("        void Save();");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            filePath = @$"{_solutionPath}\{SolutionName}.Domain\Interfaces\Repositories\IClientRepository.cs";

            file = File.Create(filePath);
            
            file.Close();
            
            sb = new StringBuilder();
            
            #region Assemble crud client repository

            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine($"using {SolutionName}.Domain.Models;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Domain.Interfaces.Repositories");
            sb.AppendLine("{");
            sb.AppendLine("    public interface IClientRepository : ICrudRepository<Client>");
            sb.AppendLine("    {");
            sb.AppendLine("        Task<IEnumerable<Client>> GetAllClientByNameAsync(string name);");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }
    
        private void AddDapperRepositories()
        {
            Console.WriteLine("Start dapper repositories creation");

            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.Dapper\Repositories");

            string filePath = @$"{_solutionPath}\{SolutionName}.Dapper\Repositories\UnitOfWork.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assemble unit of work class

            sb.AppendLine("using System.Data;");
            sb.AppendLine($"using {SolutionName}.Dapper.Factory;");
            sb.AppendLine($"using {SolutionName}.Domain.Enums;");
            sb.AppendLine($"using {SolutionName}.Domain.Interfaces.Repositories;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Dapper.Repositories");
            sb.AppendLine("{");
            sb.AppendLine("    public class UnitOfWork : IUnitOfWork");
            sb.AppendLine("    {");
            sb.AppendLine("        public readonly IDbConnection _dbConnection;");
            sb.AppendLine("");
            sb.AppendLine("        private IDbTransaction _transaction = null;");
            sb.AppendLine("");
            sb.AppendLine("        public UnitOfWork(IDbConnectionFactory dbConnectionFactory)");
            sb.AppendLine("        {");
            sb.AppendLine("            _dbConnection = dbConnectionFactory.CreateDBConnection(EnumDatabaseConnection.DBDefault);");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public UnitOfWork(IDbConnection dbConnection)");
            sb.AppendLine("        {");
            sb.AppendLine("            _dbConnection = dbConnection;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public IDbConnection DbConnection => _dbConnection;");
            sb.AppendLine("");
            sb.AppendLine("        public IDbTransaction DbTransaction => _transaction;");
            sb.AppendLine("");
            sb.AppendLine("        public void Begin()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (_dbConnection.State == ConnectionState.Closed) _dbConnection.Open();");
            sb.AppendLine("");
            sb.AppendLine("            if (_transaction != null) return;");
            sb.AppendLine("");
            sb.AppendLine("            _transaction = _dbConnection.BeginTransaction();");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public void Commit()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (_transaction != null)");
            sb.AppendLine("            {");
            sb.AppendLine("                try");
            sb.AppendLine("                {");
            sb.AppendLine("                    _transaction.Commit();");
            sb.AppendLine("                    Dispose();");
            sb.AppendLine("                }");
            sb.AppendLine("                catch");
            sb.AppendLine("                {");
            sb.AppendLine("                    Rollback();");
            sb.AppendLine("                    throw;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public void Dispose()");
            sb.AppendLine("        {");
            sb.AppendLine("            _transaction?.Dispose();");
            sb.AppendLine("            _transaction = null;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public void Rollback()");
            sb.AppendLine("        {");
            sb.AppendLine("            _transaction.Rollback();");
            sb.AppendLine("            Dispose();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            filePath = @$"{_solutionPath}\{SolutionName}.Dapper\Repositories\CrudRepository.cs";

            file = File.Create(filePath);
            
            file.Close();
            
            sb = new StringBuilder();
            
            #region Assemble crud class

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Linq.Expressions;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Dommel;");
            sb.AppendLine($"using {SolutionName}.Domain.Interfaces.Repositories;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Dapper.Repositories");
            sb.AppendLine("{");
            sb.AppendLine("    public abstract class CrudRepository<T> : ICrudRepository<T> where T : class");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly IUnitOfWork _unitOfWork;");
            sb.AppendLine("");
            sb.AppendLine("        public CrudRepository(IUnitOfWork unitOfWork)");
            sb.AppendLine("        {");
            sb.AppendLine("            _unitOfWork = unitOfWork;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task DeleteAsync(T obj)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                _unitOfWork.Begin();");
            sb.AppendLine("");
            sb.AppendLine("                await _unitOfWork.DbConnection.DeleteAsync<T>(obj);");
            sb.AppendLine("");
            sb.AppendLine("                Save();");
            sb.AppendLine("            }");
            sb.AppendLine("            catch(Exception e)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task<IEnumerable<T>> GetAllAsync()");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                var result = await _unitOfWork.DbConnection.GetAllAsync<T>();");
            sb.AppendLine("");
            sb.AppendLine("                return result;");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task<T> GetByIdAsync(object id)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                var result = await _unitOfWork.DbConnection.GetAsync<T>(id);");
            sb.AppendLine("");
            sb.AppendLine("                return result;");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task InsertAsync(T obj)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                _unitOfWork.Begin();");
            sb.AppendLine("");
            sb.AppendLine("                await _unitOfWork.DbConnection.InsertAsync(obj, _unitOfWork.DbTransaction);");
            sb.AppendLine("");
            sb.AppendLine("                Save();");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public void Save()");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                _unitOfWork.Commit();");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {");
            sb.AppendLine("                _unitOfWork.Rollback();");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task<IEnumerable<T>> SelectAllAsync(Expression<Func<T, bool>> predicate)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                var result = await _unitOfWork.DbConnection.SelectAsync<T>(predicate);");
            sb.AppendLine("");
            sb.AppendLine("                return result;");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task<T> SelectFirstAsync(Expression<Func<T, bool>> predicate)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                var result = await _unitOfWork.DbConnection.SelectAsync<T>(predicate);");
            sb.AppendLine("");
            sb.AppendLine("                return result.FirstOrDefault();");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task UpdateAsync(T obj)");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                _unitOfWork.Begin();");
            sb.AppendLine("");
            sb.AppendLine("                await _unitOfWork.DbConnection.UpdateAsync(obj, _unitOfWork.DbTransaction);");
            sb.AppendLine("");
            sb.AppendLine("                Save();");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            filePath = @$"{_solutionPath}\{SolutionName}.Dapper\Repositories\ClientRepository.cs";

            file = File.Create(filePath);
            
            file.Close();
            
            sb = new StringBuilder();
            
            #region Assemble crud client repository class

            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine($"using {SolutionName}.Domain.Interfaces.Repositories;");
            sb.AppendLine($"using {SolutionName}.Domain.Models;");
            sb.AppendLine("using System;");
            sb.AppendLine("using Dapper;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Dapper.Repositories ");
            sb.AppendLine("{");
            sb.AppendLine("    public class ClientRepository : CrudRepository<Client>, IClientRepository ");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly IUnitOfWork _unitOfWork;");
            sb.AppendLine("        public ClientRepository (IUnitOfWork unitOfWork) : base (unitOfWork) ");
            sb.AppendLine("        {");
            sb.AppendLine("            _unitOfWork = unitOfWork;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task<IEnumerable<Client>> GetAllClientByNameAsync(string name) ");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                StringBuilder sb = new StringBuilder();");
            sb.AppendLine("                sb.Append($\"SELECT * FROM TB_CLIENTE WITH (NOLOCK) WHERE Name LIKE '%{name}%'\");");
            sb.AppendLine("");
            sb.AppendLine("                var result = await _unitOfWork.DbConnection.QueryAsync<Client>(sb.ToString());");
            sb.AppendLine("");
            sb.AppendLine("                return result;");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            Console.WriteLine();
        }
    
        private void AddHTTPCommClass()
        {
            Console.WriteLine("Start external services interfaces / repositories creation");

            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.Domain\Interfaces\Repositories\ExternalServices");
            Directory.CreateDirectory(@$"{_solutionPath}\{SolutionName}.ExternalServices\Base");

            string filePath = @$"{_solutionPath}\{SolutionName}.Domain\Interfaces\Repositories\ExternalServices\IHTTP.cs";

            var file = File.Create(filePath);
            
            file.Close();
            
            StringBuilder sb = new StringBuilder();
            
            #region Assemble http interface

            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.Domain.Interfaces.Repositories.ExternalServices");
            sb.AppendLine("{");
            sb.AppendLine("    public interface IHTTP");
            sb.AppendLine("    {");
            sb.AppendLine("        Task<string> GetAsync(string urlBase, string method, Dictionary<string, string> queryParams, Dictionary<string, string> headers);");
            sb.AppendLine("        Task<string> PostAsync(string urlBase, string method, string jsonData, Dictionary<string, string> queryParams, Dictionary<string, string> headers);");
            sb.AppendLine("        Task<string> PutAsync(string urlBase, string method, string jsonData, Dictionary<string, string> queryParams, Dictionary<string, string> headers);");
            sb.AppendLine("        Task<string> DeleteAsync(string urlBase, string method, Dictionary<string, string> queryParams, Dictionary<string, string> headers);");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());

            filePath = @$"{_solutionPath}\{SolutionName}.ExternalServices\Base\HTTP.cs";

            file = File.Create(filePath);
            
            file.Close();
            
            sb = new StringBuilder();
            
            #region Assemble http class

            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine($"using {SolutionName}.Domain.Interfaces.Repositories.ExternalServices;");
            sb.AppendLine("");
            sb.AppendLine($"namespace {SolutionName}.ExternalServices.Base ");
            sb.AppendLine("{");
            sb.AppendLine("    public class HTTP : IHTTP ");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly IHttpClientFactory _httpClientFactory;");
            sb.AppendLine("        public HTTP (IHttpClientFactory httpClientFactory) ");
            sb.AppendLine("        {");
            sb.AppendLine("            _httpClientFactory = httpClientFactory;");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task<string> GetAsync(string urlBase, string method, Dictionary<string, string> queryParams, Dictionary<string, string> headers) ");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                string url = urlBase;");
            sb.AppendLine("");
            sb.AppendLine("                if (!string.IsNullOrEmpty(method))");
            sb.AppendLine("                    url += method;");
            sb.AppendLine("");
            sb.AppendLine("                if (queryParams != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    url += \"?\";");
            sb.AppendLine("");
            sb.AppendLine("                    string outVal = string.Empty;");
            sb.AppendLine("                    StringBuilder sb = new StringBuilder();");
            sb.AppendLine("");
            sb.AppendLine("                    foreach(var key in queryParams.Keys)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        queryParams.TryGetValue(key, out outVal);");
            sb.AppendLine("                        sb.Append(key + \"=\" + outVal + \"&\");");
            sb.AppendLine("                    }");
            sb.AppendLine("");
            sb.AppendLine("                    url += sb.ToString();");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var request = new HttpRequestMessage(HttpMethod.Get, url);");
            sb.AppendLine("");
            sb.AppendLine("                if (headers != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    string outVal = string.Empty;");
            sb.AppendLine("");
            sb.AppendLine("                    foreach(var key in queryParams.Keys)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        queryParams.TryGetValue(key, out outVal);");
            sb.AppendLine("                        request.Headers.Add(key, outVal);");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                string responseString = string.Empty;");
            sb.AppendLine("");
            sb.AppendLine("                using (var client = _httpClientFactory.CreateClient())");
            sb.AppendLine("                {");
            sb.AppendLine("                    var response = await client.SendAsync(request);");
            sb.AppendLine("                    responseString = await response.Content.ReadAsStringAsync();");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                return responseString;");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {  ");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task<string> PostAsync(string urlBase, string method, string jsonData, Dictionary<string, string> queryParams, Dictionary<string, string> headers) ");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                string url = urlBase;");
            sb.AppendLine("");
            sb.AppendLine("                if (!string.IsNullOrEmpty(method))");
            sb.AppendLine("                    url += method;");
            sb.AppendLine("");
            sb.AppendLine("                if (queryParams != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    url += \"?\";");
            sb.AppendLine("");
            sb.AppendLine("                    string outVal = string.Empty;");
            sb.AppendLine("                    StringBuilder sb = new StringBuilder();");
            sb.AppendLine("");
            sb.AppendLine("                    foreach(var key in queryParams.Keys)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        queryParams.TryGetValue(key, out outVal);");
            sb.AppendLine("                        sb.Append(key + \"=\" + outVal + \"&\");");
            sb.AppendLine("                    }");
            sb.AppendLine("");
            sb.AppendLine("                    url += sb.ToString();");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var request = new HttpRequestMessage(HttpMethod.Post, url);");
            sb.AppendLine("");
            sb.AppendLine("                if (!string.IsNullOrEmpty(jsonData))");
            sb.AppendLine("                    request.Content = new StringContent(jsonData, Encoding.UTF8, \"application/json\");");
            sb.AppendLine("");
            sb.AppendLine("                if (headers != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    string outVal = string.Empty;");
            sb.AppendLine("");
            sb.AppendLine("                    foreach(var key in queryParams.Keys)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        queryParams.TryGetValue(key, out outVal);");
            sb.AppendLine("                        request.Headers.Add(key, outVal);");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                string responseString = string.Empty;");
            sb.AppendLine("");
            sb.AppendLine("                using (var client = _httpClientFactory.CreateClient())");
            sb.AppendLine("                {");
            sb.AppendLine("                    var response = await client.SendAsync(request);");
            sb.AppendLine("                    responseString = await response.Content.ReadAsStringAsync();");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                return responseString;");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {  ");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task<string> PutAsync(string urlBase, string method, string jsonData, Dictionary<string, string> queryParams, Dictionary<string, string> headers) ");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                string url = urlBase;");
            sb.AppendLine("");
            sb.AppendLine("                if (!string.IsNullOrEmpty(method))");
            sb.AppendLine("                    url += method;");
            sb.AppendLine("");
            sb.AppendLine("                if (queryParams != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    url += \"?\";");
            sb.AppendLine("");
            sb.AppendLine("                    string outVal = string.Empty;");
            sb.AppendLine("                    StringBuilder sb = new StringBuilder();");
            sb.AppendLine("");
            sb.AppendLine("                    foreach(var key in queryParams.Keys)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        queryParams.TryGetValue(key, out outVal);");
            sb.AppendLine("                        sb.Append(key + \"=\" + outVal + \"&\");");
            sb.AppendLine("                    }");
            sb.AppendLine("");
            sb.AppendLine("                    url += sb.ToString();");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var request = new HttpRequestMessage(HttpMethod.Put, url);");
            sb.AppendLine("");
            sb.AppendLine("                if (!string.IsNullOrEmpty(jsonData))");
            sb.AppendLine("                    request.Content = new StringContent(jsonData, Encoding.UTF8, \"application/json\");");
            sb.AppendLine("");
            sb.AppendLine("                if (headers != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    string outVal = string.Empty;");
            sb.AppendLine("");
            sb.AppendLine("                    foreach(var key in queryParams.Keys)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        queryParams.TryGetValue(key, out outVal);");
            sb.AppendLine("                        request.Headers.Add(key, outVal);");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                string responseString = string.Empty;");
            sb.AppendLine("");
            sb.AppendLine("                using (var client = _httpClientFactory.CreateClient())");
            sb.AppendLine("                {");
            sb.AppendLine("                    var response = await client.SendAsync(request);");
            sb.AppendLine("                    responseString = await response.Content.ReadAsStringAsync();");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                return responseString;");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {  ");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("");
            sb.AppendLine("        public async Task<string> DeleteAsync(string urlBase, string method, Dictionary<string, string> queryParams, Dictionary<string, string> headers) ");
            sb.AppendLine("        {");
            sb.AppendLine("            try");
            sb.AppendLine("            {");
            sb.AppendLine("                string url = urlBase;");
            sb.AppendLine("");
            sb.AppendLine("                if (!string.IsNullOrEmpty(method))");
            sb.AppendLine("                    url += method;");
            sb.AppendLine("");
            sb.AppendLine("                if (queryParams != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    url += \"?\";");
            sb.AppendLine("");
            sb.AppendLine("                    string outVal = string.Empty;");
            sb.AppendLine("                    StringBuilder sb = new StringBuilder();");
            sb.AppendLine("");
            sb.AppendLine("                    foreach(var key in queryParams.Keys)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        queryParams.TryGetValue(key, out outVal);");
            sb.AppendLine("                        sb.Append(key + \"=\" + outVal + \"&\");");
            sb.AppendLine("                    }");
            sb.AppendLine("");
            sb.AppendLine("                    url += sb.ToString();");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                var request = new HttpRequestMessage(HttpMethod.Delete, url);");
            sb.AppendLine("");
            sb.AppendLine("                if (headers != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    string outVal = string.Empty;");
            sb.AppendLine("");
            sb.AppendLine("                    foreach(var key in queryParams.Keys)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        queryParams.TryGetValue(key, out outVal);");
            sb.AppendLine("                        request.Headers.Add(key, outVal);");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                string responseString = string.Empty;");
            sb.AppendLine("");
            sb.AppendLine("                using (var client = _httpClientFactory.CreateClient())");
            sb.AppendLine("                {");
            sb.AppendLine("                    var response = await client.SendAsync(request);");
            sb.AppendLine("                    responseString = await response.Content.ReadAsStringAsync();");
            sb.AppendLine("                }");
            sb.AppendLine("");
            sb.AppendLine("                return responseString;");
            sb.AppendLine("            }");
            sb.AppendLine("            catch (Exception e)");
            sb.AppendLine("            {  ");
            sb.AppendLine("                throw;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            #endregion
        
            File.WriteAllText(filePath, sb.ToString());
            
            Console.WriteLine();
        }
    }
}
