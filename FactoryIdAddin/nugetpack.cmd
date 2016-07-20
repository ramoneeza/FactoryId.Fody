ParseAsmInfo ./bin/release/FactoryIdAddin.Fody.dll -p FactoryIdAddin.Fody.nuspec_tpl -o *.*.nuspec
ParseAsmInfo ./bin/release/FactoryIdAddin.Fody.dll -p nugetpush.cmd_tpl -o *.cmd
nuget pack FactoryIdAddin.Fody.nuspec -Prop Configuration=Release -outputdirectory ../NugetBuild/
