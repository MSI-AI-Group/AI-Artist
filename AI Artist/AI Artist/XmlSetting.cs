using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace AI_Artist_V2
{
	[Serializable]
	public class XmlSetting
	{
		[XmlIgnore]
		public static XmlSetting Current { get; set; } = new XmlSetting();

		public bool IsEnableWildcards { set; get; } = true;
		public bool IsEnableDynamicPrompts { set; get; } = true;

		public int RecentMaxCount { set; get; } = 20;


	}
}