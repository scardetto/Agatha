require 'albacore'

version = "2.0.5"
build_configuration = "Release"
feed = 'https://www.myget.org/F/is3inc/api/v2/package'
api_key = 'bc95f2a2-bbc8-45ec-abad-304dd1c35fea'

task :copy do
  FileUtils.cp Dir.glob("build/#{build_configuration}/Agatha.Common.*"), 'nuget/Agatha.Common/lib/net461'
  FileUtils.cp Dir.glob("build/#{build_configuration}/Agatha.ServiceLayer.*"), 'nuget/Agatha.ServiceLayer/lib/net461'
  FileUtils.cp Dir.glob("build/#{build_configuration}/Agatha.StructureMap.*"), 'nuget/Agatha.StructureMap/lib/net461'
end

task :package => [:copy] do
  sh 'nuget\\.nuget\\NuGet.exe pack nuget\\Agatha.Common\\Agatha.Common.nuspec -OutputDirectory nuget'
  sh 'nuget\\.nuget\\NuGet.exe pack nuget\\Agatha.ServiceLayer\\Agatha.ServiceLayer.nuspec -OutputDirectory nuget'
  sh 'nuget\\.nuget\\NuGet.exe pack nuget\\Agatha.StructureMap\\Agatha.StructureMap.nuspec -OutputDirectory nuget'
end

task :publish => [:copy] do
  sh "./nuget/.nuget/NuGet.exe push ./nuget/Agatha.Common.#{version}.nupkg #{api_key} -Source #{feed}"
  sh "./nuget/.nuget/NuGet.exe push ./nuget/Agatha.ServiceLayer.#{version}.nupkg #{api_key} -Source #{feed}"
  sh "./nuget/.nuget/NuGet.exe push ./nuget/Agatha.StructureMap.#{version}.nupkg #{api_key} -Source #{feed}"
end

task :default => [:test]
