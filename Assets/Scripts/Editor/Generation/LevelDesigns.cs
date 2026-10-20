using System.Collections.Generic;

namespace Ecos.EditorTools.Generation
{
    /// <summary>
    /// Design das três fases. Referência de física do Lumo: pulo alcança ~3,6 células de altura;
    /// andando cruza vãos de até 3 células e correndo até 5-6 células.
    /// </summary>
    public static class LevelDesigns
    {
        public static List<LevelData> All() => new List<LevelData> { Level1(), Level2(), Level3() };

        /// <summary>FASE 1 — Floresta Encantada (fácil): apresenta andar, pular, plataformas, inimigo e corrida.</summary>
        public static LevelData Level1()
        {
            var L = new LevelData("Level1", 1, "Floresta Encantada", Theme.Forest, 180, 18);

            // Início: andar e pular.
            L.Start(2, 3);
            L.Ground(0, 29, 3);
            L.Sign(5, 3, "Use A/D ou as setas ←/→ para andar.");
            L.Crystals(8, 10, 3);
            L.Sign(12, 3, "Pressione ESPAÇO para pular!");
            L.Ground(15, 18, 4);
            L.Ground(19, 22, 5);
            L.Crystal(16, 5).Crystal(17, 5);
            L.Crystal(20, 6).Crystal(21, 6);
            L.Crystals(25, 27, 3);

            // Primeiro buraco e plataformas de madeira.
            L.Crystal(30, 5).Crystal(31, 5);
            L.Ground(32, 42, 3);
            L.Sign(33, 3, "Pule por baixo das plataformas de madeira para subir nelas.\nSegure ↓ + ESPAÇO para descer.");
            L.Plat(35, 5, 4);
            L.Crystals(35, 38, 6);
            L.Plat(39, 8, 3);
            L.Crystal(40, 9);

            // Primeiro inimigo.
            L.Ground(46, 60, 3);
            L.Sign(47, 3, "Pule em cima do Lodo Sombrio para derrotá-lo!");
            L.Slime(55, 3);
            L.Crystal(50, 3).Crystal(51, 3);
            L.Crystal(58, 3).Crystal(59, 3);

            // Escadaria natural até um platô.
            L.Ground(61, 63, 4);
            L.Ground(64, 66, 5);
            L.Ground(67, 75, 6);
            L.Crystal(70, 7).Crystal(71, 8).Crystal(72, 7);
            L.Ground(76, 78, 4);
            L.Ground(79, 92, 3);

            // Checkpoint e introdução à corrida.
            L.Checkpoint(81, 3);
            L.Plat(85, 5, 3);
            L.Crystal(86, 6);
            L.Sign(89, 3, "Segure SHIFT para correr e alcançar pulos mais longos!");
            L.Crystal(94, 5).Crystal(95, 6).Crystal(96, 5);
            L.Ground(98, 110, 3);
            L.Sign(100, 3, "Cuidado com os espinhos!");
            L.Spikes(104, 105, 3);
            L.Crystal(104, 5).Crystal(105, 5);
            L.Slime(108, 3);

            // Travessia por plataformas sobre o abismo.
            L.Plat(112, 4, 3);
            L.Crystal(113, 5);
            L.Plat(117, 5, 3);
            L.Crystal(118, 7);
            L.Plat(122, 4, 3);
            L.Crystal(123, 5);
            L.Plat(127, 3, 3);
            L.Crystal(128, 4);
            L.Ground(131, 150, 3);
            L.Spikes(136, 137, 3);
            L.Crystal(136, 5).Crystal(137, 5);
            L.Crystal(140, 3).Crystal(141, 3);
            L.Slime(146, 3);

            // Final: subida até o portal.
            L.Ground(151, 153, 4);
            L.Ground(154, 156, 5);
            L.Ground(157, 179, 5);
            L.Plat(160, 7, 3);
            L.Crystals(160, 162, 8);
            L.Crystal(167, 5).Crystal(168, 5);
            L.Goal(173, 5);
            return L;
        }

        /// <summary>FASE 2 — Cavernas Luminosas (média): escadas, espinhos, buracos, morcegos e plataforma móvel.</summary>
        public static LevelData Level2()
        {
            var L = new LevelData("Level2", 2, "Cavernas Luminosas", Theme.Cave, 190, 20);
            L.Block(0, 189, 18, 19); // teto da caverna

            L.Start(2, 3);
            L.Ground(0, 11, 3);
            L.Sign(4, 3, "Use W/S ou as setas ↑/↓ para subir e descer escadas.");
            L.Ladder(11, 3, 9);
            L.Crystal(11, 6);
            L.Ground(12, 20, 10);
            L.Crystals(14, 16, 10);

            // Espinhos, lodo e o primeiro morcego.
            L.Ground(21, 45, 3);
            L.Spikes(26, 27, 3);
            L.Slime(32, 3);
            L.Spikes(37, 39, 3);
            L.Crystal(38, 6);
            L.Bat(42, 6, 2.5);
            L.Crystal(48, 6);

            // Checkpoint e torre com escadas.
            L.Ground(50, 62, 3);
            L.Checkpoint(51, 3);
            L.Ladder(55, 3, 7);
            L.Plat(56, 7, 5);
            L.Crystals(57, 59, 8);
            L.Ladder(62, 3, 10);
            L.Block(63, 72, 0, 10);
            L.Spikes(67, 68, 11);
            L.Crystal(67, 13).Crystal(68, 13);
            L.Bat(70, 14, 2);

            // Plataforma móvel sobre o abismo.
            L.Ground(73, 80, 6);
            L.Crystals(75, 77, 6);
            L.Sign(78, 6, "Espere a plataforma móvel chegar e suba nela!");
            L.Mover(81, 6, 3, 0);
            L.Ground(87, 100, 6);
            L.Checkpoint(89, 6);
            L.Slime(95, 6);
            L.Crystal(97, 6);

            // Escada suspensa sobre espinhos.
            L.Sign(98, 6, "Pule e segure ↑ para agarrar a escada no ar!");
            L.Ground(101, 104, 2);
            L.Spikes(101, 104, 2, true);
            L.Crystal(102, 8);
            L.Ladder(103, 7, 13);
            L.Crystal(103, 11);
            L.Plat(104, 13, 4);
            L.Bat(110, 16, 2);
            L.Block(110, 118, 0, 12);
            L.Crystal(112, 13).Crystal(113, 13);
            L.Heart(116, 13);

            // Espinhos, morcego e salto com corrida.
            L.Ground(119, 126, 4);
            L.Spikes(123, 124, 4);
            L.Bat(125, 8, 3);
            L.Crystal(129, 7);
            L.Ground(132, 189, 4);
            L.Checkpoint(134, 4);

            // Corredor de teto baixo com espinhos.
            L.Sign(139, 4, "Espinhos no teto: use pulos curtos (solte o ESPAÇO rápido).");
            L.Block(141, 152, 9, 19);
            L.CeilingSpikes(144, 147, 8);
            L.Crystals(144, 147, 4);
            L.Slime(150, 4);

            // Reta final.
            L.Plat(158, 6, 3);
            L.Crystal(159, 7);
            L.Plat(162, 9, 3);
            L.Crystal(163, 10).Crystal(164, 10);
            L.Bat(167, 7, 3);
            L.Spikes(171, 172, 4);
            L.Slime(177, 4);
            L.Crystals(179, 181, 4);
            L.Goal(185, 4);
            return L;
        }

        /// <summary>FASE 3 — Ruínas da Energia (difícil): blocos que caem, esferas de espinhos, mais inimigos e o Cristal Primordial.</summary>
        public static LevelData Level3()
        {
            var L = new LevelData("Level3", 3, "Ruínas da Energia", Theme.Ruins, 216, 24);

            L.Start(2, 3);
            L.Ground(0, 14, 3);
            L.Sign(4, 3, "Blocos rachados desmoronam logo depois de pisados. Não pare!");

            // Fosso de espinhos com blocos que caem.
            L.Ground(15, 30, 1);
            L.Spikes(15, 30, 1, true);
            L.Crumble(16, 17, 3);
            L.Crumble(21, 22, 4);
            L.Crumble(26, 27, 3);
            L.Crystal(17, 5).Crystal(21, 6).Crystal(22, 6).Crystal(27, 5);

            // Espinhento.
            L.Ground(31, 45, 3);
            L.Sign(32, 3, "O Espinhento não pode ser pisado: pule por cima dele!");
            L.Crystals(34, 35, 3);
            L.Thorn(40, 3);

            // Plataforma móvel com esfera de espinhos.
            L.Mover(46, 3, 7, 0);
            L.Saw(50, 6, 0, 2);
            L.Crystal(48, 4).Crystal(53, 4);
            L.Ground(56, 72, 3);
            L.Checkpoint(57, 3);
            L.Saw(62, 3, 6, 0);
            L.Crystals(64, 66, 6);

            // Torre e travessia longa.
            L.Ladder(72, 3, 8);
            L.Crystal(72, 6);
            L.Block(73, 78, 0, 8);
            L.Bat(76, 12, 1.5);
            L.Mover(79, 9, 6, 0);
            L.Bat(83, 12, 3);
            L.Crystal(84, 10);
            L.Block(88, 100, 0, 8);
            L.Checkpoint(90, 9);
            L.Crystals(92, 93, 9);
            L.Thorn(97, 9);

            // Corredor de armadilhas.
            L.Ground(101, 115, 6);
            L.Crystal(103, 6);
            L.Spikes(105, 106, 6);
            L.Saw(108, 6, 0, 3);
            L.Spikes(111, 112, 6);
            L.Crystal(114, 6);

            // Ponte que desmorona.
            L.Crumble(116, 127, 5);
            L.Crystal(118, 6).Crystal(120, 6).Crystal(122, 6).Crystal(124, 6).Crystal(126, 6);
            L.Bat(122, 9, 3);
            L.Ground(128, 150, 6);
            L.Heart(130, 6);
            L.Checkpoint(133, 6);
            L.Slime(138, 6);
            L.Crystals(140, 141, 6);
            L.Thorn(145, 6);

            // Escalada com esfera de espinhos passando pela escada.
            L.Sign(147, 6, "Suba a escada quando a esfera de espinhos se afastar!");
            L.Ladder(150, 6, 15);
            L.Saw(145, 10, 5, 0);
            L.Crystal(150, 13);
            L.Block(151, 160, 0, 15);
            L.Crystals(152, 153, 16);
            L.Thorn(157, 16);
            L.Crystal(163, 16);
            L.Block(166, 172, 0, 12);
            L.Checkpoint(168, 13);

            // Desafio final e altar do Cristal Primordial.
            L.Ground(173, 200, 4);
            L.Slime(177, 4);
            L.Spikes(181, 182, 4);
            L.Crystals(184, 185, 4);
            L.Bat(186, 7, 3);
            L.Thorn(190, 4);
            L.Saw(194, 4, 4, 0);
            L.Crystal(192, 6);
            L.Ground(201, 203, 5);
            L.Ground(204, 206, 6);
            L.Ground(207, 215, 7);
            L.Crystal(202, 5).Crystal(205, 6);
            L.FinalCrystal(211, 7);
            return L;
        }
    }
}
