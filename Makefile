## Change to your KSP installation
KSP_FILES="/opt/SteamLibrary/steamapps/common/Kerbal Space Program/"

REFERENCES=UnityEngine,UnityEngine.UI,Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine.CoreModule,UnityEngine.AssetBundleModule,UnityEngine.PhysicsModule,UnityEngine.InputLegacyModule,${KSP_FILES}/GameData/Kopernicus/Plugins/Kopernicus.dll,${KSP_FILES}/GameData/Kopernicus/Plugins/Kopernicus.Parser.dll

Parallax.dll: Loader/QuadMeshDictionary.cs Loader/ParallaxSource.cs
	mcs -lib:${KSP_FILES}/KSP_Data/Managed/ -sdk:2 -r:${REFERENCES} -out:Parallax.dll -optimize Loader/QuadMeshDictionary.cs Loader/ParallaxSource.cs -target:library

PQSModExpansion.dll: Parallax.dll Loader/SubdivisionPQSMod.cs
	mcs -lib:${KSP_FILES}/KSP_Data/Managed/ -sdk:2 -r:${REFERENCES},Parallax.dll -optimize -out:PQSModExpansion.dll  Loader/SubdivisionPQSMod.cs -target:library

all: Parallax.dll PQSModExpansion.dll
clean:
	rm -f Parallax.dll PQSModExpansion.dll

install: Parallax.dll PQSModExpansion.dll
	cp Parallax.dll PQSModExpansion.dll ${KSP_FILES}/GameData/Parallax/
