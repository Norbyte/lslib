mkdir Release\Packed
mkdir Release\Packed\Tools

copy RconClient\bin\Release\net8.0\*.config Release\Packed\Tools\
copy RconClient\bin\Release\net8.0\*.runtimeconfig.json Release\Packed\Tools\
copy RconClient\bin\Release\net8.0\*.dll Release\Packed\Tools\
copy RconClient\bin\Release\net8.0\*.exe Release\Packed\Tools\

copy StoryCompiler\bin\Release\net8.0\*.config Release\Packed\Tools\
copy StoryCompiler\bin\Release\net8.0\*.runtimeconfig.json Release\Packed\Tools\
copy StoryCompiler\bin\Release\net8.0\*.dll Release\Packed\Tools\
copy StoryCompiler\bin\Release\net8.0\*.exe Release\Packed\Tools\

copy VTexTool\bin\Release\net8.0\*.config Release\Packed\Tools\
copy VTexTool\bin\Release\net8.0\*.runtimeconfig.json Release\Packed\Tools\
copy VTexTool\bin\Release\net8.0\*.dll Release\Packed\Tools\
copy VTexTool\bin\Release\net8.0\*.exe Release\Packed\Tools\

copy StoryDecompiler\bin\Release\net8.0\*.config Release\Packed\Tools\
copy StoryDecompiler\bin\Release\net8.0\*.runtimeconfig.json Release\Packed\Tools\
copy StoryDecompiler\bin\Release\net8.0\*.dll Release\Packed\Tools\
copy StoryDecompiler\bin\Release\net8.0\*.exe Release\Packed\Tools\

copy DebuggerFrontend\bin\Release\net8.0\*.config Release\Packed\Tools\
copy DebuggerFrontend\bin\Release\net8.0\*.runtimeconfig.json Release\Packed\Tools\
copy DebuggerFrontend\bin\Release\net8.0\*.dll Release\Packed\Tools\
copy DebuggerFrontend\bin\Release\net8.0\*.exe Release\Packed\Tools\

copy StatParser\bin\Release\net8.0\*.config Release\Packed\Tools\
copy StatParser\bin\Release\net8.0\*.runtimeconfig.json Release\Packed\Tools\
copy StatParser\bin\Release\net8.0\*.dll Release\Packed\Tools\
copy StatParser\bin\Release\net8.0\*.exe Release\Packed\Tools\

copy Divine\bin\Release\net8.0\*.config Release\Packed\Tools\
copy Divine\bin\Release\net8.0\*.runtimeconfig.json Release\Packed\Tools\
copy Divine\bin\Release\net8.0\*.dll Release\Packed\Tools\
copy Divine\bin\Release\net8.0\*.exe Release\Packed\Tools\

copy ConverterApp\bin\Release\net8.0-windows\*.config Release\Packed\
copy ConverterApp\bin\Release\net8.0-windows\*.runtimeconfig.json Release\Packed\
copy ConverterApp\bin\Release\net8.0-windows\*.dll Release\Packed\
copy ConverterApp\bin\Release\net8.0-windows\*.exe Release\Packed\

pause