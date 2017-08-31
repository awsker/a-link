using System.Collections.Generic;
using System.ComponentModel;

namespace alink.Models
{
    public class RulesConfig : INotifyPropertyChanged
    {
        private string _description;
        public string Filename { get; set; }

        public string Description
        {
            get { return _description; }
            set
            {
                if (value != _description)
                {
                    _description = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Description"));
                }
            }
        }

        public IList<MemoryRule> Rules { get; }
        public long Checksum { get; set; }

        public RulesConfig(string description)
        {
            _description = description.Replace("|", "");
            Rules = new BindingList<MemoryRule>();
            Checksum = 0;
        }

        public RulesConfig(string description, IEnumerable<MemoryRule> rules):this(description)
        {
            foreach (var r in rules)
                Rules.Add(r);
            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Description;
        }
    
    }
}
