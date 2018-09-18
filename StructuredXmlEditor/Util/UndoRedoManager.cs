using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

//-----------------------------------------------------------------------
public class UndoRedoDescription
{
	public bool IsUndo { get; set; }
	public bool IsRedo { get; set; }
	public string Description { get; set; }
	public int CountFromCurrent { get; set; }
	public bool IsSavePoint { get; set; }
	public bool IsCurrentPoint { get; set; }

	public UndoRedoDescription()
	{

	}
}

//-----------------------------------------------------------------------
public interface IUndoRedoAction
{
	string Desc { get; }
	void Do();
	void Undo();
}

//-----------------------------------------------------------------------
public interface IValueChangeAction
{
	string ValueName { get; }
	object KeyObj { get; }
}
public class ValueChangeAction<T> : IValueChangeAction, IUndoRedoAction
{
	public object oldData;
	public object newData;

	public T oldVal;
	public T newVal;

	public Action<T, object> setter;
	public string ValueName { get; set; }
	public object KeyObj { get; set; }

	public string Desc { get { return "Change " + ValueName + " (" + oldVal + " -> " + newVal + ")"; } }

	public ValueChangeAction(T oldVal, T newVal, Action<T, object> setter, object keyObj, string valueName)
	{
		this.oldVal = oldVal;
		this.newVal = newVal;
		this.setter = setter;
		this.ValueName = valueName;
		this.KeyObj = keyObj;
	}

	public bool IsChange
	{
		get
		{
			if (newVal == null)
			{
				return oldVal != null;
			}
			else
			{
				return !newVal.Equals(oldVal);
			}
		}
	}

	public void Do()
	{
		setter(newVal, newData);
	}

	public void Undo()
	{
		setter(oldVal, oldData);
	}
}

//-----------------------------------------------------------------------
public class UndoRedoManager : NotifyPropertyChanged
{
	public int GroupingMS { get; set; } = 500;

	public Stack<UndoRedoGroup> UndoStack = new Stack<UndoRedoGroup>();
	public Stack<UndoRedoGroup> RedoStack = new Stack<UndoRedoGroup>();

	public IEnumerable<UndoRedoDescription> DescriptionStack
	{
		get
		{
			var temp = new List<UndoRedoDescription>();
			int count = 0;
			foreach (var item in UndoStack)
			{
				temp.Add(new UndoRedoDescription()
				{
					Description = item.Desc,
					IsUndo = true,
					IsSavePoint = item == savePoint,
					IsCurrentPoint = count == 0,
					CountFromCurrent = count++
				});
			}

			temp.Reverse();
			foreach (var item in temp) yield return item;

			count = 1;
			foreach (var item in RedoStack)
			{
				yield return new UndoRedoDescription()
				{
					Description = item.Desc,
					IsSavePoint = item == savePoint,
					IsRedo = true,
					CountFromCurrent = count++
				};
			}
		}
	}

	public bool IsModified
	{
		get
		{
			if (savePoint == null)
			{
				return UndoStack.Count != 0;
			}
			else
			{
				return UndoStack.Count == 0 ? true : UndoStack.Peek() != savePoint;
			}
		}
	}

	public bool CanUndo
	{
		get { return UndoStack.Count > 0; }
	}

	public bool CanRedo
	{
		get { return RedoStack.Count > 0; }
	}

	public UndoRedoManager()
	{

	}

	public UsingContext DisableUndoScope()
	{
		--enableUndoRedo;

		return new UsingContext(() =>
		{
			++enableUndoRedo;
		});
	}

	public UsingContext ActionScope(string name)
	{
		var oldName = overrideName;
		overrideName = name;

		return new UsingContext(() =>
		{
			overrideName = oldName;
		});
	}

	public void DoValueChange<T>(object keyObj, T prevValue, object prevData, T newValue, object newData, Action<T, object> setter, string valueName)
	{
		ValueChangeAction<T> desc = null;

		if (enableUndoRedo != 0)
		{
			desc = new ValueChangeAction<T>(prevValue, newValue, setter, DateTime.Now, valueName);
			desc.oldData = prevData;
			desc.newData = newData;
			desc.Do();
			return;
		}

		// collapse into existing
		var collapsed = false;
		if (UndoStack.Count > 0)
		{
			var lastGroup = UndoStack.Peek();

			var currentTime = DateTime.Now;
			var diff = (currentTime - lastGroup.LastActionTime).TotalMilliseconds;

			if (diff <= GroupingMS)
			{
				foreach (var action in lastGroup.Actions)
				{
					var valueChange = action as ValueChangeAction<T>;
					if (valueChange != null)
					{
						if (valueChange.KeyObj == keyObj && valueChange.ValueName == valueName)
						{
							valueChange.newVal = newValue;
							valueChange.newData = newData;
							valueChange.Do();

							lastGroup.LastActionTime = currentTime;

							collapsed = true;
						}
					}
				}
			}
		}

		if (!collapsed)
		{
			desc = new ValueChangeAction<T>(prevValue, newValue, setter, keyObj, valueName);
			desc.oldData = prevData;
			desc.newData = newData;

			AddUndoRedoAction(desc);
		}
		
	}

	public void ApplyDoUndo(Action _do, Action _undo, string _desc = "")
	{
		if (overrideName != null) _desc = overrideName;

		var action = new UndoRedoAction(_do, _undo, _desc);

		AddUndoRedoAction(action);
	}

	public void AddUndoRedoAction(IUndoRedoAction action)
	{
		if (enableUndoRedo == 0)
		{
			if (isInApplyUndo)
			{
				Message.Show("Nested ApplyDoUndo calls! This is bad!", "Undo Redo borked", "Ok");
				action.Do();
				return;
			}

			isInApplyUndo = true;

			action.Do();

			RedoStack.Clear();

			if (UndoStack.Count > 0)
			{
				var lastGroup = UndoStack.Peek();

				var currentTime = DateTime.Now;
				var diff = (currentTime - lastGroup.LastActionTime).TotalMilliseconds;

				if (diff <= GroupingMS)
				{
					lastGroup.Actions.Add(action);
					lastGroup.LastActionTime = currentTime;
				}
				else
				{
					var group = new UndoRedoGroup();
					group.Actions.Add(action);
					group.LastActionTime = currentTime;
					UndoStack.Push(group);
				}
			}
			else
			{
				var group = new UndoRedoGroup();
				group.Actions.Add(action);
				group.LastActionTime = DateTime.Now;
				UndoStack.Push(group);
			}

			RaisePropertyChangedEvent("IsModified");
			RaisePropertyChangedEvent("CanUndo");
			RaisePropertyChangedEvent("CanRedo");
			RaisePropertyChangedEvent("DescriptionStack");

			isInApplyUndo = false;
		}
		else
		{
			action.Do();
		}
	}

	public void Undo(int count)
	{
		for (int i = 0; i < count; i++)
		{
			Undo();
		}
	}

	public void Redo(int count)
	{
		for (int i = 0; i < count; i++)
		{
			Redo();
		}
	}

	public void Undo()
	{
		if (UndoStack.Count > 0)
		{
			var group = UndoStack.Pop();
			group.Undo();
			RedoStack.Push(group);

			RaisePropertyChangedEvent("IsModified");
			RaisePropertyChangedEvent("CanUndo");
			RaisePropertyChangedEvent("CanRedo");
			RaisePropertyChangedEvent("DescriptionStack");
		}
	}

	public void Redo()
	{
		if (RedoStack.Count > 0)
		{
			var group = RedoStack.Pop();
			group.Do();
			UndoStack.Push(group);

			RaisePropertyChangedEvent("IsModified");
			RaisePropertyChangedEvent("CanUndo");
			RaisePropertyChangedEvent("CanRedo");
			RaisePropertyChangedEvent("DescriptionStack");
		}
	}

	public void MarkSavePoint()
	{
		if (UndoStack.Count > 0)
		{
			savePoint = UndoStack.Peek();
		}
		else
		{
			savePoint = null;
		}

		RaisePropertyChangedEvent("IsModified");
		RaisePropertyChangedEvent("DescriptionStack");
	}

	int enableUndoRedo;
	UndoRedoGroup savePoint;
	bool isInApplyUndo;
	string overrideName;
}

//-----------------------------------------------------------------------
public class UndoRedoGroup
{
	public List<IUndoRedoAction> Actions { get; set; } = new List<IUndoRedoAction>();
	public DateTime LastActionTime { get; set; }

	public void Do()
	{
		foreach (var action in Actions) action.Do();
	}

	public void Undo()
	{
		var reversed = Actions.ToList();
		reversed.Reverse();
		foreach (var action in reversed) action.Undo();
	}

	public string Desc
	{
		get
		{
			return string.Join(",", Actions.Select(e => e.Desc.Replace("\n", "")).Distinct());
		}
	}
}

//-----------------------------------------------------------------------
public class UndoRedoAction : IUndoRedoAction
{
	public Action DoAction { get; set; }
	public Action UndoAction { get; set; }
	public string Desc { get; set; }

	public UndoRedoAction(Action _do, Action _undo, string _desc)
	{
		this.DoAction = _do;
		this.UndoAction = _undo;
		this.Desc = _desc;
	}

	public void Do()
	{
		DoAction();
	}

	public void Undo()
	{
		UndoAction();
	}
}

//-----------------------------------------------------------------------
public class UsingContext : IDisposable
{
	private Action action;

	public UsingContext(Action action)
	{
		this.action = action;
	}

	public void Dispose()
	{
		action();
	}
}
