using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace StructuredXmlEditor
{
	public static class Future
	{
		private static object m_locker = new object();
		private static Dictionary<object, Tuple<Action, Timer>> m_futures = new Dictionary<object, Tuple<Action, Timer>>();

		public static void Call(Action func, int ms, object key = null)
		{
			if (key == null) key = new object();

			lock (m_locker)
			{
				if (m_futures.ContainsKey(key))
				{
					m_futures[key].Item2.Change(ms, Timeout.Infinite);
				}
				else
				{
					var timer = new Timer(TimerElapsed, key, ms, Timeout.Infinite);
					m_futures[key] = new Tuple<Action, Timer>(func, timer);
				}
			}
		}

		private static void TimerElapsed(object key)
		{
			lock (m_locker)
			{
				var data = m_futures[key];
				m_futures.Remove(key);

				Application.Current?.Dispatcher?.BeginInvoke(data.Item1);
				data.Item2.Dispose();
			}
		}
	}
}
