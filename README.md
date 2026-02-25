# revit-mcp-commandset 🔴

> [!WARNING]
> Deprecation notice, this repo is being archived in favour of the mono repo approch for the mcp https://github.com/mcp-servers-for-revit/mcp-servers-for-revit
> Simpler CI, less confusion for noobs, npm package and simpler install


---
---



🔄 Revit-MCP Client | Core implementation of the Revit-MCP protocol that connects LLMs with Revit. Includes essential CRUD commands for Revit elements enabling AI-driven BIM automation.

# Custom Commands Setup

## Installation

1. Create folder: `RevitMCPCommandSet` at the end of the usual Revit addins directory like so `C:\Users\[USERNAME]\AppData\Roaming\Autodesk\Revit\Addins\20XX\RevitMCPCommandSet\`

2. Add files:
   - Copy `command.json` from this repo to the `RevitMCPCommandSet` folder
   - Create `20XX` subfolder
   - Place compiled output from this repo in the `20XX` subfolder

3. In Revit: Go to **Add-ins** > **Settings** > **Refresh** > **Save**

## Important Note

   - Command names must be identical between `revit-mcp` and `revit-mcp-commandset` repositories, otherwise Claude cannot find them.
   - The `commandRegistry.json` is created automatically, do not import it from the installer.
