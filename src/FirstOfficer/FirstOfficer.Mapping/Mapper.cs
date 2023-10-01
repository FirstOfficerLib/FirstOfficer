using System.Collections;
using System.Reflection;

namespace FirstOfficer.Mapping
{
    public class Mapper : IMapper
    {
        private readonly Dictionary<string, MethodInfo> _mappingMethods = new();
        public Mapper()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var mapperMethods = assemblies.SelectMany(a => a.GetTypes()).Where(t => t.GetCustomAttribute<FirstOfficerMapperAttribute>() != null)
                .SelectMany(a => a.GetMethods(BindingFlags.Public | BindingFlags.Static)).Where(a=> a.GetParameters().Length == 1);

            foreach (var method in mapperMethods)
            {
                _mappingMethods.Add(GetKey(method), method);
            }
        }

        private string GetKey(MethodInfo method)
        {
            return $"{method.GetParameters().First().ParameterType.FullName}:{method.ReturnType.FullName}";
        }

        public T Map<T>(object source)
        {
            var sourceType = source.GetType();
            var targetType = typeof(T);

            var sourceName = sourceType.FullName!;
            var targetName = targetType.FullName!;

            if (sourceType.IsGenericType)
            {
                sourceName = sourceType.GetGenericArguments().First().FullName!;
            }
            if (targetType.IsGenericType)
            {
                targetName = targetType.GetGenericArguments().First().FullName!;
            }

            var key  = $"{sourceName}:{targetName}";

            if (!_mappingMethods.ContainsKey(key))
            {
                throw new ArgumentException($"Cannot map {source.GetType()} to {typeof(T)} as there is no known type mapping", nameof(source));
            }

            var method = _mappingMethods[key];

            if (!sourceType.IsGenericType)
            {
                return ((T)method.Invoke(null, new[] { source })!)!;
            }
            
            var rtn = (IList)Activator.CreateInstance(targetType)!;

            foreach (var item in (IEnumerable)source)
            {
                rtn.Add(method.Invoke(null, new[] { item })!);
            }   
            
            return (T)rtn;
        }
    }
}