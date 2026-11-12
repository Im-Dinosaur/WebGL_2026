using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZombieCqb
{
    internal static class ZombieCqbSceneArt
    {
        private const string artFolder = "Assets/ZombieCQB/Art"; //생성한 게임 그림의 위치
        private const int tile = 32; //한 셀을 그리는 픽셀 수
        private static readonly Color32 ink = rgb(13, 20, 25); //외형의 어두운 윤곽

        internal static Sprite pixel() //단색 영역과 게이지에 사용할 스프라이트
        {
            Paint image = new Paint(4, 4); //단색 원본 그림
            image.rect(0, 0, 4, 4, Color.white);
            return save("Pixel", image, 4);
        }

        internal static Sprite actor(string name, Color32 coat, Color32 head, bool zombie, bool guard) //위에서 본 시민, 좀비, 경비 외형 제작
        {
            Paint image = new Paint(40, 40); //캐릭터 원본 그림
            image.ellipse(20, 16, 17, 14, rgb(9, 16, 20, 140));
            image.rect(9, 5, 8, 12, ink);
            image.rect(23, 5, 8, 12, ink);
            image.rect(11, 7, 5, 7, shade(coat, -48));
            image.rect(24, 7, 5, 7, shade(coat, -48));
            image.ellipse(20, 18, 15, 11, ink);
            image.ellipse(20, 19, 13, 9, coat);
            image.rect(8, 18, 24, 5, shade(coat, 26));
            image.rect(4, 19, 7, 10, ink);
            image.rect(29, 19, 7, 10, ink);
            image.rect(6, 20, 5, 7, coat);
            image.rect(29, 20, 5, 7, coat);
            image.ellipse(20, 26, 9, 10, ink);
            image.ellipse(20, 27, 7, 8, head);
            image.rect(14, 30, 12, 3, shade(head, 22));

            if (guard)
            {
                image.rect(13, 30, 14, 4, rgb(116, 149, 160));
                image.rect(14, 29, 12, 3, rgb(26, 43, 51));
                image.rect(29, 24, 7, 4, rgb(99, 131, 145));
                image.rect(33, 26, 4, 8, rgb(49, 71, 83));
                image.rect(8, 18, 5, 4, rgb(238, 113, 75));
            }
            else if (zombie)
            {
                image.rect(14, 28, 4, 3, rgb(207, 255, 159));
                image.rect(23, 28, 4, 3, rgb(207, 255, 159));
                image.rect(18, 32, 4, 2, rgb(83, 133, 84));
                image.rect(8, 25, 4, 5, head);
                image.rect(28, 25, 4, 5, head);
            }
            else
            {
                image.rect(13, 29, 14, 5, rgb(74, 51, 42));
                image.rect(16, 30, 8, 4, rgb(99, 70, 51));
                image.rect(17, 13, 6, 4, shade(coat, 37));
            }

            return save(name, image, 40);
        }

        internal static Sprite door() //닫힌 문짝과 손잡이 그림
        {
            Paint image = new Paint(tile, tile); //문짝 원본 그림
            image.rect(0, 2, tile, 28, rgb(17, 23, 29));
            image.rect(1, 5, 30, 22, rgb(97, 103, 92));
            image.rect(2, 8, 28, 17, rgb(72, 83, 79));
            image.rect(2, 25, 28, 2, rgb(158, 168, 137));
            image.rect(3, 8, 2, 16, rgb(172, 151, 91));
            image.rect(27, 8, 2, 16, rgb(172, 151, 91));
            image.rect(12, 10, 8, 3, rgb(225, 195, 117));
            image.rect(13, 20, 6, 2, rgb(126, 146, 126));
            return save("Door", image, tile);
        }

        internal static Sprite marker() //발소리 방향을 보여주는 두 겹 표시
        {
            Paint image = new Paint(28, 28); //방향 표시 원본 그림
            for (int x = 3; x < 25; x++) //표시의 가로 픽셀 위치
            {
                int y = 23 - Mathf.Abs(x - 14) / 2; //화살 모양의 세로 높이
                image.rect(x, y, 1, 3, Color.white);
                image.rect(x, y - 8, 1, 2, new Color32(255, 255, 255, 135));
            }
            return save("Hearing", image, 28);
        }

        internal static Sprite liquid() //액체 감염 영역의 바닥 효과
        {
            Paint image = new Paint(64, 64); //감염 영역 원본 그림
            image.ellipse(32, 32, 30, 30, rgb(116, 222, 126, 100));
            image.ellipse(32, 32, 28, 28, rgb(77, 165, 108, 102));
            image.ellipse(33, 30, 22, 21, rgb(61, 128, 87, 104));
            for (int i = 0; i < 24; i++) //영역 안의 작은 빛무늬
            {
                int x = 9 + hash(i, 17) % 46; //빛무늬의 가로 위치
                int y = 9 + hash(i, 28) % 46; //빛무늬의 세로 위치
                if (new Vector2(x - 32, y - 32).sqrMagnitude < 25f * 25f)
                    image.rect(x, y, 2, 2, rgb(162, 255, 163, 126));
            }
            return save("Liquid", image, 64);
        }

        internal static Sprite fan() //전방 감염 발동의 짧은 범위 표시
        {
            Paint image = new Paint(64, 64); //전방 효과 원본 그림
            for (int y = 2; y < 63; y++) //전방으로 펼쳐지는 세로 범위
            {
                int half = Mathf.RoundToInt(y * 0.48f); //높이에 따른 범위의 반폭
                byte alpha = (byte)Mathf.RoundToInt(118f - y * 0.95f); //끝으로 갈수록 옅어지는 투명도
                image.rect(32 - half, y, half * 2, 1, rgb(150, 238, 172, alpha));
            }
            return save("Droplet", image, 64);
        }

        internal static Sprite map(string name, string[] rows, Vector2Int spawn, Vector2Int exit, bool second) //바닥과 벽, 가구를 한 장의 맵으로 그리기
        {
            int width = rows[0].Length; //맵의 가로 셀 수
            Paint image = new Paint(width * tile, rows.Length * tile); //맵 원본 그림
            for (int y = 0; y < rows.Length; y++) //맵의 행
            {
                for (int x = 0; x < width; x++) //맵의 열
                {
                    int left = x * tile; //현재 타일의 픽셀 왼쪽
                    int bottom = y * tile; //현재 타일의 픽셀 아래
                    char cell = rows[y][x]; //그릴 지형 문자
                    Color32 floor = floorColor(x, y, second); //현재 공간의 바닥 색
                    image.rect(left, bottom, tile, tile, floor);
                    image.rect(left, bottom, 1, tile, shade(floor, -13));
                    image.rect(left, bottom, tile, 1, shade(floor, -13));
                    image.rect(left + 1, bottom + tile - 2, tile - 1, 1, shade(floor, 6));
                    image.rect(left + 5 + hash(x, y) % 22, bottom + 5 + hash(y, x) % 22, 2, 1, shade(floor, 15));

                    if (cell == '#')
                        paintWall(image, left, bottom, second);
                    else if (cell == 'B')
                        paintFurniture(image, left, bottom, second, (x + y) % 3);
                    else if (cell == 'd' || cell == 'D')
                        paintDoorway(image, left, bottom);
                    else if (cell == 'X')
                        paintDebris(image, left, bottom, x, y);
                }
            }

            paintPad(image, spawn, false);
            paintPad(image, exit, true);
            return save(name, image, tile);
        }

        private static Color32 floorColor(int x, int y, bool second) //방과 복도의 색조 계산
        {
            bool corridor = second ? y >= 7 && y <= 11 : y >= 6 && y <= 10; //복도에 속하는지 여부
            int variation = hash(x, y) % 8 - 3; //바닥 한 칸의 색 변화
            if (corridor)
                return shade(second ? rgb(62, 78, 85) : rgb(72, 81, 78), variation);

            if ((x / 9 + (y < 6 ? 1 : 0)) % 2 == 0)
                return shade(second ? rgb(54, 74, 92) : rgb(80, 79, 67), variation);

            return shade(second ? rgb(63, 78, 77) : rgb(66, 83, 82), variation);
        }

        private static void paintWall(Paint image, int x, int y, bool second) //두께와 모서리가 보이는 벽 타일
        {
            image.rect(x, y, tile, tile, rgb(24, 31, 36));
            image.rect(x + 1, y + 7, tile - 2, tile - 8, second ? rgb(44, 61, 75) : rgb(53, 65, 66));
            image.rect(x, y + tile - 3, tile, 2, second ? rgb(86, 112, 126) : rgb(99, 119, 115));
            image.rect(x + 1, y + 6, tile - 2, 2, rgb(16, 24, 30));
            image.rect(x + 2, y + 22, tile - 4, 1, rgb(74, 92, 94));
            image.rect(x + 7, y + 12, 3, 2, rgb(33, 45, 51));
        }

        private static void paintFurniture(Paint image, int x, int y, bool second, int style) //책상과 보관함 형태의 장애물
        {
            Color32 baseColor = second ? rgb(77, 110, 130) : rgb(120, 105, 76); //장애물의 기본 색
            image.rect(x + 2, y + 2, 28, 27, rgb(21, 28, 33));
            image.rect(x + 3, y + 7, 26, 23, baseColor);
            image.rect(x + 4, y + 26, 24, 2, shade(baseColor, 32));
            image.rect(x + 5, y + 9, 22, 3, shade(baseColor, -20));
            image.rect(x + 13, y + 13, 6, 2, shade(baseColor, 40));
            if (style == 0)
            {
                image.rect(x + 9, y + 16, 14, 6, rgb(39, 52, 56));
                image.rect(x + 10, y + 19, 12, 2, rgb(115, 160, 146));
            }
            else if (style == 1)
            {
                image.rect(x + 12, y + 16, 8, 7, rgb(205, 218, 197));
                image.rect(x + 15, y + 15, 2, 9, rgb(168, 88, 65));
                image.rect(x + 12, y + 18, 8, 2, rgb(168, 88, 65));
            }
            else
            {
                image.rect(x + 8, y + 16, 16, 5, shade(baseColor, -29));
                image.rect(x + 10, y + 17, 12, 1, shade(baseColor, 36));
            }
        }

        private static void paintDoorway(Paint image, int x, int y) //열린 문 자리의 노란 경계 표시
        {
            image.rect(x + 1, y + 1, tile - 2, 2, rgb(139, 128, 88));
            image.rect(x + 1, y + tile - 3, tile - 2, 2, rgb(139, 128, 88));
            image.rect(x + 3, y + 5, 4, 4, rgb(197, 171, 105));
            image.rect(x + 25, y + 23, 4, 4, rgb(197, 171, 105));
        }

        private static void paintDebris(Paint image, int x, int y, int cellX, int cellY) //통과 가능한 바닥의 작은 표식
        {
            for (int i = 0; i < 7; i++) //작은 조각을 배치할 횟수
            {
                int localX = 3 + hash(cellX * 7 + i, cellY) % 26; //조각의 가로 위치
                int localY = 3 + hash(cellY * 9 + i, cellX) % 26; //조각의 세로 위치
                image.rect(x + localX, y + localY, 3, 2, rgb(106, 118, 112));
            }
        }

        private static void paintPad(Paint image, Vector2Int cell, bool exit) //시작 위치 또는 탈출 위치의 바닥 신호
        {
            int x = cell.x * tile; //표식 왼쪽 픽셀
            int y = cell.y * tile; //표식 아래 픽셀
            Color32 line = exit ? rgb(153, 244, 174) : rgb(114, 164, 146); //표식의 강조 색
            image.rect(x + 1, y + 1, 30, 30, rgb(41, 82, 65));
            image.rect(x + 2, y + 2, 28, 2, line);
            image.rect(x + 2, y + 28, 28, 2, line);
            image.rect(x + 2, y + 3, 2, 26, line);
            image.rect(x + 28, y + 3, 2, 26, line);
            image.rect(x + 8, y + 14, 14, 4, line);
            for (int i = 0; i < 6; i++) //출구 방향 화살의 픽셀 줄
                image.rect(x + 19 + i, y + 10 + i, 1, 12 - 2 * i, line);
        }

        private static Sprite save(string name, Paint image, float pixelsPerUnit) //스프라이트 원본과 임포트 설정 저장
        {
            Directory.CreateDirectory(artFolder);
            string path = $"{artFolder}/{name}.png"; //스프라이트 파일 경로
            Texture2D texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false); //PNG로 저장할 이미지
            texture.SetPixels32(image.pixels);
            texture.Apply(false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path); //픽셀 그림의 스프라이트 설정
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        internal static Color32 rgb(byte red, byte green, byte blue, byte alpha = 255) //픽셀용 색 생성
        {
            return new Color32(red, green, blue, alpha);
        }

        private static Color32 shade(Color32 color, int offset) //밝기 차이가 있는 색 생성
        {
            return rgb((byte)Mathf.Clamp(color.r + offset, 0, 255), (byte)Mathf.Clamp(color.g + offset, 0, 255),
                (byte)Mathf.Clamp(color.b + offset, 0, 255), color.a);
        }

        private static int hash(int x, int y) //반복해도 동일한 바닥 무늬 선택
        {
            return (int)((uint)(x * 374761393 + y * 668265263) ^ ((uint)(x * 1911520717 + y * 122949829) >> 13)) & int.MaxValue;
        }

        private sealed class Paint
        {
            internal readonly int width; //그림의 가로 픽셀 수
            internal readonly int height; //그림의 세로 픽셀 수
            internal readonly Color32[] pixels; //원본 이미지 픽셀

            internal Paint(int width, int height) //그림의 픽셀 공간 생성
            {
                this.width = width;
                this.height = height;
                pixels = new Color32[width * height];
            }

            internal void rect(int x, int y, int w, int h, Color32 color) //픽셀 사각형 그리기
            {
                for (int row = Mathf.Max(0, y); row < Mathf.Min(height, y + h); row++) //그릴 사각형의 행
                {
                    for (int col = Mathf.Max(0, x); col < Mathf.Min(width, x + w); col++) //그릴 사각형의 열
                        pixels[row * width + col] = color;
                }
            }

            internal void ellipse(int x, int y, int rx, int ry, Color32 color) //타원형 픽셀 실루엣 그리기
            {
                for (int row = -ry; row <= ry; row++) //타원 중심을 기준으로 한 세로 위치
                {
                    int span = Mathf.RoundToInt(rx * Mathf.Sqrt(1f - row * row / (float)(ry * ry))); //해당 줄의 가로 반폭
                    rect(x - span, y + row, span * 2 + 1, 1, color);
                }
            }
        }
    }
}
