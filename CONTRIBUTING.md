# MHServerEmu Contribution Guidelines

This is a reverse engineering project, and the game we are dealing with heavily relies on certain aspects being 100% the same between the client and the server. This is both a blessing and a curse: on one hand, we have access to a lot of data and logic that traditionally would be server-only; on the other hand, we cannot significantly deviate from existing implementations without unexpected side effects. We may be able to introduce more significant deviations as the project matures, but this is where things stand right now.

For this reason, most gameplay-related changes have to be made with the client in mind, and contributors are expected to be using IDA, Ghidra, or other similar disassembler software. For static analysis you should be disassembling the Mac executable, even if you are running on Windows, because the former contains debug symbols for all class and function names that are not present in the other version.

If this is something you would be able to help with, please contact us on [Discord](https://discord.gg/hjR8Bj52t3), so that we can discuss your potential contributions. If you believe you can contribute in some other way that does not directly involve reverse engineering, we can discuss this as well. We would also be glad to answer any questions about the current state of the codebase.

**Pull requests without prior discussion are most likely going to be disregarded. You have been warned.**
