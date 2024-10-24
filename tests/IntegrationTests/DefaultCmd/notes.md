# Notes

## Options on default commands

> todo: reword and integrate this into docs

Right now, the semantics of options in default commands and whether or
not they are also recognized by their parent is somewhat fuzzy -- even
more so when it comes to *hidden* commands...

So, for now, we won't test that, and we'll just say that trying to use
options from default commands (without invoking it directly) is
currently **unsupported**.

That means that they might be usable, they might not, they might make
everything explode into a thousand confettis.

Two things are guaranteed:
    - when using a command explicitly, all its options are usable,
      whether or not it is the default command of the parent group.
    - group options can still be specified when using default commands