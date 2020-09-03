using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DynamicDataStore.Core.Model
{
    public class PocoBase : INotifyPropertyChanged
    {
        public override string ToString()
        {
            List<PropertyInfo> propertyInfo = this.GetType().GetProperties()
                .Where(o => (o.PropertyType != typeof(byte[]) && !o.PropertyType.IsClass)).ToList();

            if (propertyInfo.Count < 1)
            {
                return base.ToString();
            }
            else
            {
                string[] s = propertyInfo.ConvertAll(o =>
                {
                    var propValue = o.GetValue(this, null)?? "";

                    return $"{o.Name}={propValue.ToString()}";

                }).ToArray();

                return string.Join(",", s);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(object sender, string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
