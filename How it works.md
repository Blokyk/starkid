# How it works

## Generator side

- Find a class marked `[CLI]`

- Get the app name and potential entry point from that attribute

- Check if the class is also marked with `[Description]`, and get the desc from it if that's the case

- Get all members of the class
  
  - For properties, fields and methods, check if its marked with `[Option]`


