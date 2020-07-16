using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;


namespace CentridNet.EFCoreAutoMigrator.Utilities{

    class Utilities{

        public static ModelSnapshot CompileSnapshot(Assembly migrationAssembly, Assembly dbContextAssembly, string source){
            return Compile<ModelSnapshot>(source, new HashSet<Assembly>() {
                AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "netstandard").Single(),
                typeof(object).Assembly,
                typeof(DbContext).Assembly,
                migrationAssembly,
                dbContextAssembly,
                typeof(DbContextAttribute).Assembly,
                typeof(ModelSnapshot).Assembly,
                typeof(SqlServerValueGenerationStrategy).Assembly,
                typeof(AssemblyTargetedPatchBandAttribute).Assembly,
                typeof(NpgsqlDatabaseFacadeExtensions).Assembly
            });
        }
        
        private static T Compile<T>(string source, IEnumerable<Assembly> references) 
        {
            var options = CSharpParseOptions.Default
                .WithLanguageVersion(LanguageVersion.Latest);

            var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);

            var compilation = CSharpCompilation.Create("Dynamic",
                new[] { SyntaxFactory.ParseSyntaxTree(source, options) },
                references.Select(a => MetadataReference.CreateFromFile(a.Location)),
                compileOptions
            );

            using var ms = new MemoryStream();
            var e = compilation.Emit(ms);
            if (!e.Success)
                throw new Exception("Compilation failed");
            ms.Seek(0, SeekOrigin.Begin);

            var context = new AssemblyLoadContext(null, true);
            var assembly = context.LoadFromStream(ms);

            var modelType = assembly.DefinedTypes.Where(t => typeof(T).IsAssignableFrom(t)).Single();

            return (T)Activator.CreateInstance(modelType);
        }

        public static async Task<byte[]> CompressSource(string source)
        {           
            using var dbStream = new MemoryStream();
            using (var blobStream = new GZipStream(dbStream, CompressionLevel.Fastest, true))
            {
                await blobStream.WriteAsync(Encoding.UTF8.GetBytes(source));
            }
            dbStream.Seek(0, SeekOrigin.Begin);

            return dbStream.ToArray();
        }

         public static async Task<string> DecompressSource(byte[] source){
             if (source != null){
                using var stream = new GZipStream(new MemoryStream(source), CompressionMode.Decompress);
                return await new StreamReader(stream).ReadToEndAsync();
            }
            return null;
         }

        public static IEnumerable<MethodInfo> GetExtensionMethods(string extensionMethod, Type extendedType)
        {
            return from asm in AppDomain.CurrentDomain.GetAssemblies()
                        from type in asm.GetTypes()
                            where type.IsSealed && !type.IsGenericType && !type.IsNested
                                from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                where method.Name == extensionMethod
                                where method.IsDefined(typeof(ExtensionAttribute), false)
                                where method.GetParameters()[0].ParameterType == extendedType 
                                select method;

        }
    }
}