using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Lists all items for a provider

namespace lib
{
    public interface Lister
    {
        Task<Category> CategoryTree();
    }

    public class Category
    {
        public string Name { get; set; }
        public List<Category> SubCategories { get; set; }
        public List<Feature> Features { get; set; }
    }

    public class Feature
    {
        public string Name { get; set; }
        public FeatureType FeatureType { get; set; }
    }

    public class FeatureType
    {
        public BaseType BaseType { get; set; }

        public FeatureTypeOptions Options { get; set; }
    }

    public enum BaseType : ushort
    {
        Integer = 0,
        Decimal = 1,
        String = 2,
        Boolean = 3,
        Enum = 4,
    }

    public class FeatureTypeOptions
    {
        public string Regex { get; set; }
        public Dictionary<string, object> ProviderDefinition { get; set; } 
    }
}
