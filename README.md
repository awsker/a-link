# A-Link
This application can synchronize parts of a process' memory between clients that are connected to the same server.

# Usage
Select a process in the top combo box. In the 'Memory Offset' combo box below, create or select a memory offset. This is not required, but can be useful if the memory addresses you intend to reference are dynamic. You can either enter the address to a pointer (32- or 64-bit), or enter an absolute offset that all your rules will use. Now you can hit the 'Attach' button to start listening to the process.

Below is a simple lobby for hosting and joining. At the moment this requires the server user to know their networking (ie. have their port forwarding in order). All users in the same lobby should ideally use the same ruleset. A client will only accept memory changes to a memory address with the same offset as one in their own ruleset.

Very little security is otherwise offered in terms of denying malicious clients access to a server (ie. no password system yet). Only send your IP to people you know and trust!

At the bottom of the application is where you select the 'ruleset' to use, that is, what set of memory addresses to listen for changes on and what to do in case they change. Create your own ruleset or import one into the /rules directory of the application.

# Setting up rules
Every rule consists of a couple of fields.
* Description - a descriptive text
* Memory offset - offset to the address, entered as a hexadecimal value (0x can be omitted).
* #bytes - number of bytes to read. For integers, only 1, 2, 4 and 8 are valid numbers. For decimals, only 4 and 8 are valid numbers.
* Data Type - what data type the bytes represent. Use 'data' for any binary data.
* When to send - If data type is integer or decimal, this setting can be used to only send data if the value increases or decreases. If the data type is Flags, this can be set to only send if a bit flag changes to on or off. This also has the added effect of only accepting the same type of change from other clients.
* What to send - Can be set to 'All bytes' or 'Difference'. The meaning of 'Difference' changes depending on the data type. In the case of 'data' or 'Flags', 'Difference' will only send the bytes that actually changed. This is best used for large number of bytes where you want to avoid (as best as possible) having two or more clients modifying the same data and have them overwrite eachother, even though they aren't touching the same bytes. In the case of numbers, 'Difference' only sends the numerical difference and lets the clients change their values. This is best used for frequently changed values where you don't necessarily need absolute synchronization. When using this option, incoming changes to these addresses will be clamped to their data types' min- and max values so as to avoid wrapping. 
* [Endianness](https://en.wikipedia.org/wiki/Endianness)
* Log - if checked, a line will be printed to the chat when the memory at this address was changed

# The settings files
I kept the settings file as simple and readable as possible. They are plain txt-files with settings separated by linebreaks, so they can be edited outside of the application with ease. Very little error handling is provided if you break the formatting.

# The code
While functional, it's a proof of concept more than anything. Exception handling, structure and documentation is sub-par.
It is provided as is. No warranties of any kind are offered. For process memory manipulation I use the [extemory](https://github.com/jeffora/extemory) library. 
