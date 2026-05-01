using System.Collections.Generic;
using MinorShift.Emuera.Runtime.Script.Statements;

namespace MinorShift.Emuera.Runtime.Script;

internal sealed class ExecutionContext
{
    public FunctionLabelLine Function { get; }
    public long[] LocalIntegers { get; }
    public string[] LocalStrings { get; }
    public long[] ArgIntegers { get; }
    public string[] ArgStrings { get; }

    private ExecutionContext _parent;
    private readonly List<ExecutionContext> _children = new();

    public ExecutionContext(FunctionLabelLine func, ExecutionContext parent)
    {
        Function = func;
        _parent = parent;
        _parent?._children.Add(this);

        LocalIntegers = new long[func.LocalLength];
        LocalStrings = new string[func.LocalsLength];
        ArgIntegers = new long[func.ArgLength];
        ArgStrings = new string[func.ArgsLength];
    }

    public ExecutionContext Parent => _parent;

    public void Dispose()
    {
        _parent?._children.Remove(this);
        foreach (var child in _children)
            child._parent = null;
        _children.Clear();
    }
}
