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
		private static Dictionary<object, FutureData> m_futures = new Dictionary<object, FutureData>();

		public static void Call(Action func, int ms, object key = null)
		{
			if (key == null) { key = new object(); }

			lock (m_locker)
			{
				FutureData existing;
				if (m_futures.TryGetValue(key, out existing))
				{
					existing.func = func;
					existing.remainingDelayMS = ms;
				}
				else
				{
					m_futures[key] = new FutureData(key, func, ms);
				}
			}
		}

		static Future()
		{
			new Thread(() =>
			{
				Thread.CurrentThread.IsBackground = true;

				DateTime lastTime = DateTime.Now;
				while (true)
				{ 
					Thread.Sleep(10);

					lock (m_locker)
					{
						DateTime currentTime = DateTime.Now;
						var expired = (int)(currentTime - lastTime).TotalMilliseconds;

						foreach (var data in m_futures.Values.ToList())
						{
							data.remainingDelayMS -= expired;
							if (data.remainingDelayMS <= 0)
							{
								m_futures.Remove(data.key);
							}

							data.func();
						}

						lastTime = currentTime;
					}
				}
			}).Start();
		}

		private class FutureData
		{
			public object key;
			public Action func;
			public int remainingDelayMS;

			public FutureData(object key, Action func, int delayMS)
			{
				this.key = key;
				this.func = func;
				this.remainingDelayMS = delayMS;
			}
		}
	}
}
