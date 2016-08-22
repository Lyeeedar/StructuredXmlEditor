using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//-----------------------------------------------------------------------
public class UndoRedoManager : NotifyPropertyChanged
{
	public int GroupingMS { get; set; } = 500;

	public Stack<UndoRedoGroup> UndoStack = new Stack<UndoRedoGroup>();
	public Stack<UndoRedoGroup> RedoStack = new Stack<UndoRedoGroup>();

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
				return UndoStack.Peek() != savePoint;
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

	public UsingContext DisableUndoScope()
	{
		--enableUndoRedo;

		return new UsingContext(() =>
		{
			++enableUndoRedo;
		});
	}

	public void ApplyDoUndo(Action _do, Action _undo, string _desc = "")
	{
		if (enableUndoRedo == 0)
		{
			if (isInApplyUndo)
			{
				throw new Exception("Nested ApplyDoUndo calls! This is bad!");
			}

			isInApplyUndo = true;

			_do();

			RedoStack.Clear();

			var action = new UndoRedoAction(_do, _undo, _desc);

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
					group.LastActionTime = DateTime.Now;
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

			isInApplyUndo = false;
		}
		else
		{
			_do();
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
	}

	int enableUndoRedo;
	UndoRedoGroup savePoint;
	bool isInApplyUndo;
}

//-----------------------------------------------------------------------
public class UndoRedoGroup
{
	public List<UndoRedoAction> Actions { get; set; } = new List<UndoRedoAction>();
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
}

//-----------------------------------------------------------------------
public class UndoRedoAction
{
	public Action Do { get; set; }
	public Action Undo { get; set; }
	public string Desc { get; set; }

	public UndoRedoAction(Action _do, Action _undo, string _desc)
	{
		this.Do = _do;
		this.Undo = _undo;
		this.Desc = _desc;
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