using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZombieCqb
{
    public static class ZombieCqbWebBuild
    {
        public const string outputFolder = "docs/zombie-cqb"; //이 게임의 WebGL 결과 위치

        [MenuItem("Zombie CQB/Build WebGL")]
        public static void build() //좀비 게임 씬을 독립 WebGL 빌드로 내보내기
        {
            if (!File.Exists(ZombieCqbSceneSetup.scenePath))
                throw new InvalidOperationException("Create the Zombie CQB scene first.");

            BuildPlayerOptions options = new BuildPlayerOptions //전용 씬과 빌드 위치
            {
                scenes = new[] { ZombieCqbSceneSetup.scenePath },
                locationPathName = outputFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options); //WebGL 빌드 결과
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Zombie CQB build failed: " + report.summary.result);

            writeWebPage();
            Debug.Log($"ZOMBIE_CQB_BUILD_OK: {report.summary.totalSize} bytes");
        }

        public static void createAndBuild() //배치 실행에서 씬 준비와 빌드 연결
        {
            if (!File.Exists(ZombieCqbSceneSetup.scenePath))
                ZombieCqbSceneSetup.createPrototype();
            build();
        }

        private static void writeWebPage() //빌드 결과 파일에 게임 전용 웹 화면 연결
        {
            string[] files = Directory.GetFiles(Path.Combine(outputFolder, "Build")).Select(Path.GetFileName).ToArray(); //Unity가 내보낸 빌드 파일 이름
            string loader = files.Single(file => file.EndsWith(".loader.js", StringComparison.Ordinal)); //게임을 불러올 스크립트
            string prefix = loader.Substring(0, loader.Length - ".loader.js".Length); //빌드 결과의 공통 파일 이름
            string shell = File.ReadAllText(ZombieCqbSceneSetup.root + "/Web/ZombieCqbWeb.html"); //게임용 브라우저 화면
            shell = shell.Replace("@LOADER@", "Build/" + loader)
                .Replace("@DATA@", "Build/" + findOutput(files, prefix + ".data"))
                .Replace("@FRAMEWORK@", "Build/" + findOutput(files, prefix + ".framework.js"))
                .Replace("@CODE@", "Build/" + findOutput(files, prefix + ".wasm"));
            File.WriteAllText(Path.Combine(outputFolder, "index.html"), shell);
        }

        private static string findOutput(string[] files, string name) //빌드 압축 형식이 붙은 파일을 찾기
        {
            string file = files.FirstOrDefault(candidate => candidate == name || candidate == name + ".unityweb"
                || candidate == name + ".gz" || candidate == name + ".br"); //실제 생성된 데이터 또는 코드 파일
            return file ?? throw new FileNotFoundException(name);
        }
    }
}
