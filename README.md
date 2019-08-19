![](/ShadedAltKey.png)


Alt+Cmd+Store demonstrates how to implement a command-sourced data store that supports branching. This involves a simplified set of git-like functions for working with a collection of command streams that feed into the creation of data models.

## Applications

This is a work in progress that has a long way to go. The initial plan involves three applications:

* **AltCmd** - a command line interface for working with the branch structure (much like the git command line).

* **AltExplorer** - a sample application used to compose different views of a file system.

* **AltCad** - a more complex sample application involving the definition of points and lines that are positioned relative to other shapes in a geometric model. 
 

## Command Line

The AltCmd application will provide a set of commands for managing branches. The intent will be familiar to anyone who has used git.

* **Init** - creates a brand new command store

* **Clone** - creates a copy of a command store

* **Branch** - creates a new local branch

* **Checkout** - specifies the branch you want to work with

* **Merge** - incorporates changes from another local branch

* **Fetch** - refreshes a store by retrieving recent changes from an upstream store

* **Push** - sends recent commands to an upstream store


## Command Storage

Commands are defined by a set of data entry instructions, serialized as JSON strings using one of 3 storage classes:

* **FileStore** is used to store command data as files on the local file system. This makes it easy to examine the content of a command store.

* **SQLiteStore** is used to hold command data as part of a SQLite database on the local file system.

* **MemoryStore** is a transient store. If an application adds commands to this type of store, they will not persist when the application exits.


