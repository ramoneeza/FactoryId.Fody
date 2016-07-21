ParseAsmInfo ./bin/release/FactoryId.Fody.dll -p FactoryId.Fody.nuspec_tpl -o *.*.nuspec
ParseAsmInfo ./bin/release/FactoryId.Fody.dll -p nugetpush.cmd_tpl -o *.cmd
nuget pack FactoryId.Fody.nuspec -Prop Configuration=Release -outputdirectory ../NugetBuild/
