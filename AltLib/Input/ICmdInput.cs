using System;

namespace AltLib
{
    /// <summary>
    /// Base for interfaces that specify different types of command input.
    /// </summary>
    /// <remarks>
    /// Command input is held in an instance of <see cref="CmdData"/>,
    /// which is no more than a dictionary of objects, keyed by
    /// property name. These property names should be specified
    /// so that they match the name of interface properties.
    /// <para/>
    /// For example, the <see cref="ICreateBranch"/> interface
    /// extends <see cref="ICmdInput"/> and specifies an
    /// <see cref="ICreateBranch.Name"/> property. The recommended
    /// way to define the property is like this:
    /// <code>
    /// 
    ///   string name = "MyBranch";
    ///   cmdData.Add(nameof(ICreateBranch.Name), name);
    ///   
    /// </code>
    /// You can subsequently retrieve the property via a dictionary
    /// extension method, like this:
    /// <code>
    /// 
    ///   string name = cmdData.GetValue&lt;string&gt;(nameof(ICreateBranch.Name));
    ///   
    /// </code>
    /// Given that <see cref="CmdData"/> is declared as a partial class, it
    /// is also possible to simplify property access by coding additional methods
    /// to explicitly implement a command interface. For example, the source file
    /// called <c>ICreateBranchHelper.cs</c> implements the methods defined by
    /// <see cref="ICreateBranch"/>, making it possible to simplify the
    /// above to this:
    /// <code>
    /// 
    ///   string name = (cmdData as ICreateBranch).Name;
    ///   
    /// </code>
    /// <para/>
    /// Taking this approach helps to avoid "magic strings", making it
    /// easy to find references to specific command elements, while
    /// also avoiding the need to work with classes that extend
    /// <see cref="CmdData"/>.
    /// </remarks>
    public interface ICmdInput
    {
    }
}
