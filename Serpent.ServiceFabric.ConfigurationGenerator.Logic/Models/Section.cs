namespace Serpent.ServiceFabric.ConfigurationGenerator.Logic.Models
{
    using System.Collections.Generic;

    public class Section
    {
        public string Name { get; set; }

        public IEnumerable<Parameter> Parameters { get; set; }
    }
}