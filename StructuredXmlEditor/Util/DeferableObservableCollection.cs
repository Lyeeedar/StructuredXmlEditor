using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//-----------------------------------------------------------------------
public class DeferableObservableCollection<T> : ObservableCollection<T>
{
	//-----------------------------------------------------------------------
	public bool IsChanging { get; private set; }

	//-----------------------------------------------------------------------
	public DeferableObservableCollection()
	{

	}

	//-----------------------------------------------------------------------
	public DeferableObservableCollection(IEnumerable<T> collection)
		: base(collection)
	{

	}

	//-----------------------------------------------------------------------
	public void BeginChange()
	{
		IsChanging = true;
	}

	//-----------------------------------------------------------------------
	public void EndChange()
	{
		IsChanging = false;

		base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
	}

	//-----------------------------------------------------------------------
	protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		if (!IsChanging)
		{
			base.OnCollectionChanged(e);
		}
	}

	//-----------------------------------------------------------------------
	public void AddRange(IEnumerable<T> newItems, bool deferChange = false)
	{
		if (deferChange)
		{
			BeginChange();
		}

		foreach (var i in newItems)
		{
			Add(i);
		}

		if (deferChange)
		{
			EndChange();
		}
	}

	//-----------------------------------------------------------------------
	public void ReplaceWith(IEnumerable<T> newItems, bool deferChange = false)
	{
		if (deferChange)
		{
			BeginChange();
		}

		Clear();

		foreach (var i in newItems)
		{
			Add(i);
		}

		if (deferChange)
		{
			EndChange();
		}
	}
}
