using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DescendantPropertyChangedEventArgs
{
	public string PropertyName { get; set; }
	public Dictionary<string, string> Data { get; } = new Dictionary<string, string>();
}
