using System.Collections.Generic;
using System.ComponentModel;

namespace alink.Models
{
    public class RulesConfig
    {
        public string Filename { get; set; }
        public string Description { get; set; }
        public IList<MemoryRule> Rules { get; }
        public long Checksum { get; set; }

        public RulesConfig(string description)
        {
            Description = description.Replace("|", "");
            Rules = new BindingList<MemoryRule>();
            Checksum = 0;
        }

        public RulesConfig(string description, IEnumerable<MemoryRule> rules):this(description)
        {
            foreach (var r in rules)
                Rules.Add(r);
            
        }

        public override string ToString()
        {
            return Description;
        }
    
    }
}
