using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace Mute.Moe.Sigil
{
    public class Sigil
    {
        public static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";
        private static readonly string[,] Prefixes = {
            { "7a365cc2", "3cdcdb8c", null, null },
            { "7a365cc2", "3cdcdb8c", "fe5cf0f4", null },
            { "fbf3a00b", null, null, null },
            { "e3621cc2", null, null, null },
            { "b0ac0b82", "fe5cf0f4", "82610c41", "b7b090cf" },
            { "de3bc3d1", "7faaca8c", "18f6b23e", null },
            { "7a365cc2", "d842a67d", null, null },
            { "e3621cc2", "7faaca8c", "cf042a60", null },
            { "e3621cc2", "7faaca8c", "e6117eb6", null },
            { "a5c31631", "869bd8d8", "f3633360", null },
            { "82ff5e94", "2befa75a", "e5fae2fb", "cf0f809a" },
            { "e3621cc2", "2befa75a", "2f5059f3", "c3bebfe6" },
            { "e3621cc2", "869bd8d8", "36a4b4a8", "5dfed122" },
            { "e3621cc2", "7faaca8c", "1c9d4f11", "5dfed122" },
            { "e3621cc2", "869bd8d8", "ad042183", null },
            { "7a365cc2", "e5fae2fb", "4d0779c1", null },
            { "7a365cc2", "82e8305", null, null },
            { "7a365cc2", "869bd8d8", "b193cb04", null },
            { "7a365cc2", "36a4b4a8", "869bd8d8", "5ba1ba9b" },
            { "b0ac0b82", "1b1fba11", null, null },
            { "82ff5e94", "7faaca8c", "2befa75a", "1735a791" },
            { "e3621cc2", "e5fae2fb", "2f5059f3", null },
            { "7a365cc2", "36a4b4a8", "869bd8d8", "cf0f809a" },
            { "7a365cc2", "3cdcdb8c", "acc8c8fb", null },
            { "b0ac0b82", "869bd8d8", null, null },
            { "b0ac0b82", "fe5cf0f4", null, null },
            { "b0ac0b82", "54112535", null, null },
            { "7a365cc2", "869bd8d8", "1735a791", null },
            { "fbf3a00b", "48176644", null, null },
            { "82ff5e94", "2befa75a", "e5fae2fb", "fe4199fc" },
            { "b0ac0b82", "e5fae2fb", "2f5059f3", null },
            { "e3621cc2", "2befa75a", "2f5059f3", "a2880ff1" },
            { "7a365cc2", "869bd8d8", "2befa75a", null },
            { "fbf3a00b", "fe5cf0f4", "cbdce7a4", null },
            { "e3621cc2", "869bd8d8", "57ed51b0", "2ab4bc02" },
            { "fbf3a00b", "7faaca8c", "18f6b23e", null },
            { "fbf3a00b", "fe5cf0f4", "603da034", "8d560b8b" },
            { "de3bc3d1", "587b58fb", "7faaca8c", null },
            { "e3621cc2", "afc30081", null, null },
            { "7a365cc2", "869bd8d8", "fb651938", null },
            { "e3621cc2", "7faaca8c", "2befa75a", "5dfed122" },
            { "6cb74008", "66b0fffd", null, null },
            { "e3621cc2", "2d91e72f", null, null },
            { "e3621cc2", "7faaca8c", "5dfed122", null },
            { "b0ac0b82", "36a4b4a8", "b193cb04", null },
            { "7a365cc2", "869bd8d8", "82610c41", null },
            { "6cb74008", "7faaca8c", "cf0f809a", null },
            { "b0ac0b82", "36a4b4a8", null, null },
            { "e3621cc2", "7faaca8c", "5e841ec3", null },
            { "b0ac0b82", "e5fae2fb", "10545ed3", null },
            { "b0ac0b82", "36a4b4a8", "9e7ddd3d", null },
            { "b0ac0b82", "e5fae2fb", "5e841ec3", null },
            { "b0ac0b82", "fe5cf0f4", "2befa75a", "b7b090cf" },
            { "e3621cc2", "2befa75a", "b193cb04", "7faaca8c" },
            { "fbf3a00b", "fe5cf0f4", null, null },
            { "e3621cc2", "6cf35bfb", null, null },
            { "e3621cc2", "4d0779c1", null, null },
            { "e3621cc2", "2639d7d4", "a0991dbc", null },
            { "de3bc3d1", "869bd8d8", "57ed51b0", null },
            { "e3621cc2", "7191ec30", "b7b090cf", null },
            { "e3621cc2", "869bd8d8", "fd1b186c", null },
            { "fbf3a00b", "fe5cf0f4", "b7b090cf", null },
            { "e3621cc2", "869bd8d8", "fb651938", null },
            { "7a365cc2", "36a4b4a8", "869bd8d8", "ad042183" },
            { "b0ac0b82", "e5fae2fb", "824e5e1f", null },
            { "7a365cc2", "2befa75a", "b193cb04", null },
            { "e3621cc2", "10545ed3", "bddfa3f3", null },
            { "7a365cc2", "869bd8d8", "8c89b36e", null },
            { "e3621cc2", "7faaca8c", null, null },
            { "de3bc3d1", "869bd8d8", "9f3841fc", null },
            { "fbf3a00b", "7faaca8c", "cf042a60", null },
            { "de3bc3d1", "2befa75a", "b7b090cf", null },
            { "e3621cc2", "869bd8d8", "2ab4bc02", null },
            { "82ff5e94", "2befa75a", "fe5cf0f4", "b7b090cf" },
            { "e3621cc2", "4dae70af", null, null },
            { "7a365cc2", "7faaca8c", "10545ed3", "4dae70af" },
            { "362aae07", "cbdce7a4", null, null },
            { "e3621cc2", "372ac81b", null, null },
            { "e3621cc2", "e5fae2fb", null, null },
            { "fbf3a00b", "7faaca8c", null, null },
            { "de3bc3d1", "e5fae2fb", "b7b090cf", null },
            { "b0ac0b82", "5a85e6f7", null, null },
            { "b0ac0b82", "36a4b4a8", "83856b0b", null },
            { "b0ac0b82", "869bd8d8", "36a4b4a8", "52a6a74c" },
            { "7a365cc2", "869bd8d8", "d24107f2", null },
            { "b0ac0b82", "e5fae2fb", "4dae70af", "1d989f87" },
            { "e3621cc2", "7faaca8c", "824e5e1f", null },
            { "82ff5e94", "fe5cf0f4", "bddfa3f3", null },
            { "7a365cc2", "7faaca8c", "1c9d4f11", "bddfa3f3" },
            { "fbf3a00b", "e6117eb6", null, null },
            { "fbf3a00b", "9e7ddd3d", null, null },
            { "82ff5e94", "7faaca8c", null, null },
            { "e3621cc2", "66b0fffd", null, null },
            { "b0ac0b82", "36a4b4a8", "2f5059f3", null },
            { "b0ac0b82", "36a4b4a8", "1c9d4f11", null },
            { "fbf3a00b", "fe5cf0f4", "3d22f9b0", "d1fb16cc" },
            { "82ff5e94", "66b0fffd", null, null },
            { "b0ac0b82", "36a4b4a8", "fe4199fc", null },
            { "b0ac0b82", "36a4b4a8", "cb9b5189", null },
            { "82ff5e94", "87cb2bff", null, null },
            { "362aae07", "2befa75a", "fe5cf0f4", "603da034" },
            { "e3621cc2", "b7b090cf", null, null },
            { "7a365cc2", "869bd8d8", "ad7dd51b", null },
            { "b0ac0b82", "869bd8d8", "82610c41", "510344a7" },
            { "a5c31631", "8d8261d6", null, null },
            { "b0ac0b82", "869bd8d8", "4dae70af", "f0d5fe71" },
            { "e3621cc2", "869bd8d8", "57ed51b0", null },
            { "de3bc3d1", "587b58fb", "63454c71", null },
            { "28b33136", "7faaca8c", null, null },
            { "fbf3a00b", "fe5cf0f4", "cf0f809a", null },
            { "de3bc3d1", "10545ed3", "5dfed122", null },
            { "7a365cc2", "82610c41", null, null },
            { "fbf3a00b", "fe5cf0f4", "36a4b4a8", "fb5ca265" },
            { "b0ac0b82", "10545ed3", null, null },
            { "e3621cc2", "b788c92f", null, null },
            { "de3bc3d1", "e5fae2fb", "9ebd20ff", null },
            { "fbf3a00b", "fe5cf0f4", "603da034", "f3633360" },
            { "e3621cc2", "e5fae2fb", "869bd8d8", "69daeb1a" },
            { "e3621cc2", "8d8261d6", null, null },
            { "e3621cc2", "869bd8d8", "6b5e126b", null },
            { "e3621cc2", "e5fae2fb", "36a4b4a8", "5dfed122" },
            { "7a365cc2", "82610c41", "d1fb16cc", null },
            { "82ff5e94", "b7b090cf", null, null },
            { "b0ac0b82", "fe5cf0f4", "2befa75a", "7faaca8c" },
            { "82ff5e94", "7faaca8c", "387e568f", null },
            { "7a365cc2", "869bd8d8", "83856b0b", null },
            { "82ff5e94", "7faaca8c", "92e85004", null },
            { "de3bc3d1", "e5fae2fb", "adafc09c", null },
            { "fbf3a00b", "82610c41", "d1fb16cc", null },
            { "82ff5e94", "7faaca8c", "87cb2bff", null },
            { "82ff5e94", "10545ed3", "bddfa3f3", null },
            { "82ff5e94", "7faaca8c", "7191ec30", "5dfed122" },
            { "28b33136", "5a85e6f7", null, null },
            { "7a365cc2", "869bd8d8", "cf0f809a", null },
            { "e3621cc2", "cf042a60", null, null },
            { "82ff5e94", "cf042a60", null, null },
            { "b0ac0b82", "d842a67d", null, null },
            { "e3621cc2", "adafc09c", null, null },
            { "b0ac0b82", "fe5cf0f4", "2befa75a", "b193cb04" },
            { "fbf3a00b", "d6753be3", null, null },
            { "7a365cc2", "869bd8d8", "5a85e6f7", null },
            { "b0ac0b82", "7191ec30", null, null },
            { "7a365cc2", "7faaca8c", "b193cb04", "1d989f87" },
            { "de3bc3d1", "2befa75a", "b193cb04", null },
            { "b0ac0b82", "10545ed3", "b7b090cf", null },
            { "6cb74008", "6cf35bfb", null, null },
            { "362aae07", "2befa75a", "fe5cf0f4", "6acd7409" },
            { "e3621cc2", "fe5cf0f4", "54112535", null },
            { "de3bc3d1", "869bd8d8", "5a85e6f7", null },
            { "e3621cc2", "e5fae2fb", "2befa75a", "1d989f87" },
            { "e3621cc2", "4c4076d5", null, null },
            { "b0ac0b82", "7faaca8c", "4dae70af", "f0d5fe71" },
            { "362aae07", "e5fae2fb", "9ebd20ff", null },
            { "b0ac0b82", "82e8305", null, null },
            { "b0ac0b82", "10545ed3", "bddfa3f3", null },
            { "fbf3a00b", "e2f4d6e2", null, null },
            { "7a365cc2", "82610c41", "7faaca8c", "4c1ac47d" },
            { "a5c31631", "335e8694", null, null },
            { "b0ac0b82", "fe5cf0f4", "2befa75a", "e5fae2fb" },
            { "e3621cc2", "fc9ea1e2", null, null },
            { "e3621cc2", "e5fae2fb", "2befa75a", "f0d5fe71" },
            { "82ff5e94", "7faaca8c", "1c9d4f11", "5dfed122" },
            { "82ff5e94", "b193cb04", "7faaca8c", "1735a791" },
            { "28b33136", "48176644", null, null },
            { "82ff5e94", "ec96cafb", null, null },
            { "fbf3a00b", "2b187d87", null, null },
            { "fbf3a00b", "59d9e201", null, null },
            { "a5c31631", "d7e0d9e", null, null },
            { "7a365cc2", "82610c41", "869bd8d8", "4c1ac47d" },
            { "e3621cc2", "2befa75a", "1c9d4f11", "427a1751" },
            { "fbf3a00b", "b193cb04", "b0aeb7df", null },
            { "e3621cc2", "7faaca8c", "952f1d85", null },
            { "e3621cc2", "adfbec3", null, null },
            { "de3bc3d1", "fe5cf0f4", "42b4e3a3", null },
            { "e3621cc2", "e5fae2fb", "2befa75a", "b193cb04" },
            { "e3621cc2", "fe5cf0f4", "42b4e3a3", null },
            { "e3621cc2", "42b4e3a3", null, null },
            { "e3621cc2", "869bd8d8", "f0d5fe71", null },
            { "fbf3a00b", "54112535", null, null },
            { "7a365cc2", "869bd8d8", "faa6eb0", null },
            { "e3621cc2", "7faaca8c", "e53a31b5", null },
            { "e3621cc2", "1c9d4f11", "5dfed122", null },
            { "de3bc3d1", "e5fae2fb", "bddfa3f3", null },
            { "7a365cc2", "2befa75a", "7faaca8c", null },
            { "de3bc3d1", "5dfed122", null, null },
            { "7a365cc2", "2befa75a", "e5fae2fb", null },
            { "b0ac0b82", "462c645e", null, null },
            { "e3621cc2", "7faaca8c", "cf0f809a", null },
            { "b0ac0b82", "7faaca8c", "fc65290b", null },
            { "de3bc3d1", "2befa75a", "5a85e6f7", null },
            { "b0ac0b82", "fe5cf0f4", "44e1402d", null },
            { "e3621cc2", "5dfed122", null, null },
            { "fbf3a00b", "87cb2bff", null, null },
            { "b0ac0b82", "fe5cf0f4", "2befa75a", "2f5059f3" },
            { "b0ac0b82", "869bd8d8", "48176644", null },
            { "fbf3a00b", "b193cb04", "a4ed278a", null },
            { "de3bc3d1", "e5fae2fb", "87cb2bff", null },
            { "28b33136", "869bd8d8", null, null },
            { "7a365cc2", "2befa75a", "fe5cf0f4", null },
            { "b0ac0b82", "fe5cf0f4", "2befa75a", "10545ed3" },
            { "fbf3a00b", "9b3aa0b1", null, null },
            { "a5c31631", "b788c92f", null, null },
            { "fbf3a00b", "b193cb04", "b7b090cf", null },
            { "362aae07", "fe5cf0f4", "83023171", null },
            { "b0ac0b82", "2f5059f3", null, null },
            { "b0ac0b82", "4456d619", null, null },
            { "6cb74008", "b788c92f", null, null },
            { "7a365cc2", "7faaca8c", "2befa75a", "b193cb04" },
            { "7a365cc2", "7faaca8c", "82610c41", "b0aeb7df" },
            { "e3621cc2", "e5fae2fb", "5dfed122", null },
            { "b0ac0b82", "869bd8d8", "ad042183", null },
            { "e3621cc2", "e5fae2fb", "952f1d85", null },
            { "b0ac0b82", "869bd8d8", "fd1b186c", null },
            { "7a365cc2", "7faaca8c", "462c645e", "d1fb16cc" },
            { "b0ac0b82", "869bd8d8", "36a4b4a8", "fe5cf0f4" },
            { "e3621cc2", "613cb28b", null, null },
            { "7a365cc2", "869bd8d8", "510344a7", null },
            { "fbf3a00b", "42b4e3a3", null, null },
            { "e3621cc2", "2639d7d4", null, null },
            { "e3621cc2", "2befa75a", "5dfed122", null },
            { "82ff5e94", "7faaca8c", "18f6b23e", null },
            { "b0ac0b82", "36a4b4a8", "7191ec30", null },
            { "7a365cc2", "869bd8d8", "e2f4d6e2", null },
            { "b0ac0b82", "1c9d4f11", "bddfa3f3", null },
            { "28b33136", "e2f4d6e2", null, null },
            { "7a365cc2", "2befa75a", "869bd8d8", null },
            { "de3bc3d1", "e5fae2fb", "4dae70af", null },
            { "362aae07", "fe5cf0f4", "9b3aa0b1", null },
            { "e3621cc2", "fe5cf0f4", "f0d5fe71", null },
            { "7a365cc2", "82610c41", "fe5cf0f4", null },
            { "de3bc3d1", "7faaca8c", "cf042a60", null },
            { "e3621cc2", "e5fae2fb", "824e5e1f", null },
            { "b0ac0b82", "fe5cf0f4", "2befa75a", "9b3aa0b1" },
            { "82ff5e94", "6615d02d", null, null },
            { "b0ac0b82", "fe5cf0f4", "2befa75a", "9e7ddd3d" },
            { "82ff5e94", "b788c92f", null, null },
            { "fbf3a00b", "e5fae2fb", "9ebd20ff", null },
            { "fbf3a00b", "fe5cf0f4", "b193cb04", "82610c41" },
            { "e3621cc2", "7faaca8c", "36a4b4a8", "5dfed122" },
            { "7a365cc2", "82610c41", "4c4076d5", null },
            { "e3621cc2", "4dae70af", "f0d5fe71", null },
            { "de3bc3d1", "e5fae2fb", "d7e0d9e", null },
            { "de3bc3d1", "e5fae2fb", "5dfed122", null },
            { "b0ac0b82", "869bd8d8", "36a4b4a8", "a4ed278a" },
            { "82ff5e94", "613cb28b", null, null },
            { "e3621cc2", "e5fae2fb", "5e841ec3", null },
            { "82ff5e94", "e3f1ef01", null, null },
            { "b0ac0b82", "b7b090cf", null, null },
            { "82ff5e94", null, null, null },
            { "fbf3a00b", "e5fae2fb", "adafc09c", null },
            { "b0ac0b82", "fe5cf0f4", "bddfa3f3", null },
            { "b0ac0b82", "7faaca8c", "18f6b23e", null },
            { "82ff5e94", "adafc09c", null, null },
            { "e3621cc2", "81113d71", null, null },
            { "82ff5e94", "2befa75a", "fe5cf0f4", "f6bb8637" },
            { "b0ac0b82", "7faaca8c", "82610c41", "b397af9f" },
        };
        private static readonly string[,] Suffixes = {
            { "7a365cc2", null, null, null },
            { "a5c31631", "6cf35bfb", null, null },
            { "a5c31631", "462c645e", "d1fb16cc", null },
            { "82ff5e94", "10545ed3", "975397b2", null },
            { "19634ad2", "82610c41", null, null },
            { "d93ccb2", "fe5cf0f4", null, null },
            { "fbf3a00b", "b788c92f", null, null },
            { "7a365cc2", "fe5cf0f4", null, null },
            { "7a365cc2", "5ba1ba9b", null, null },
            { "fbf3a00b", "7faaca8c", "8c89b36e", null },
            { "19634ad2", "e5fae2fb", "82610c41", "b193cb04" },
            { "d93ccb2", null, null, null },
            { "82ff5e94", "6cf35bfb", null, null },
            { "d93ccb2", "9b3aa0b1", null, null },
            { "a5c31631", "6615d02d", null, null },
            { "7a365cc2", "9f680a2b", null, null },
            { "6cb74008", "7faaca8c", null, null },
            { "6cb74008", "7faaca8c", "fc65290b", null },
            { "362aae07", "7faaca8c", "d6753be3", null },
            { "19634ad2", "5ba1ba9b", null, null },
            { "fbf3a00b", "66b0fffd", null, null },
            { "fbf3a00b", "b645c8d3", null, null },
            { "19634ad2", "869bd8d8", "5ba1ba9b", null },
            { "82ff5e94", "fc65290b", null, null },
            { "a5c31631", "adfbec3", null, null },
            { "fbf3a00b", "13446f50", null, null },
            { "19634ad2", null, null, null },
            { "a5c31631", "e6117eb6", null, null },
            { "19634ad2", "e5fae2fb", "82610c41", "1735a791" },
            { "362aae07", "b193cb04", "e5fae2fb", "87cb2bff" },
            { "de3bc3d1", "7faaca8c", "2f5059f3", "3d22f9b0" },
            { "19634ad2", "10545ed3", "b7b090cf", null },
            { "7a365cc2", "b193cb04", null, null },
            { "82ff5e94", "fd1b186c", null, null },
            { "fbf3a00b", "d1fb16cc", null, null },
            { "82ff5e94", "cbdce7a4", null, null },
            { "82ff5e94", "7faaca8c", "4456d619", null },
            { "362aae07", "acc8c8fb", null, null },
            { "7a365cc2", "cf0f809a", null, null },
            { "7a365cc2", "d1fb16cc", null, null },
            { "7a365cc2", "7faaca8c", "387e568f", null },
            { "7a365cc2", "1d989f87", null, null },
            { "7a365cc2", "b7b090cf", null, null },
            { "7a365cc2", "9b3aa0b1", null, null },
            { "fbf3a00b", "7faaca8c", "cb81c430", null },
            { "6cb74008", "18f6b23e", null, null },
            { "19634ad2", "e5fae2fb", "b7b090cf", null },
            { "fbf3a00b", "eb608ee5", null, null },
            { "a5c31631", "20fd4083", null, null },
            { "7a365cc2", "36a4b4a8", "4dae70af", null },
            { "7a365cc2", "42b4e3a3", null, null },
            { "19634ad2", "1735a791", null, null },
            { "fbf3a00b", "952f1d85", null, null },
            { "a5c31631", "eb608ee5", null, null },
            { "de3bc3d1", "7faaca8c", "2f5059f3", "b3ae909c" },
            { "82ff5e94", "6245af61", null, null },
            { "a5c31631", "7faaca8c", "335e8694", null },
            { "19634ad2", "fe5cf0f4", "b7b090cf", null },
            { "de3bc3d1", "36a4b4a8", "5dfed122", null },
            { "a5c31631", null, null, null },
            { "6cb74008", "2639d7d4", "5dfed122", null },
            { "d93ccb2", "42b4e3a3", null, null },
            { "a5c31631", "fc9ea1e2", null, null },
            { "6cb74008", "ab99a1a", null, null },
            { "a5c31631", "6245af61", null, null },
            { "fbf3a00b", "372ac81b", null, null },
            { "fbf3a00b", "7faaca8c", "6245af61", null },
            { "fbf3a00b", "975397b2", null, null },
            { "a5c31631", "fe5cf0f4", null, null },
            { "7a365cc2", "10545ed3", null, null },
            { "7a365cc2", "10545ed3", "b7b090cf", null },
            { "6cb74008", "9ebd20ff", null, null },
            { "fbf3a00b", "7fcb0d8f", null, null },
            { "de3bc3d1", "7faaca8c", "7191ec30", "bddfa3f3" },
            { "19634ad2", "36a4b4a8", "869bd8d8", "5ba1ba9b" },
            { "7a365cc2", "7faaca8c", "cb81c430", null },
            { "6cb74008", "cf042a60", null, null },
            { "a5c31631", "d6753be3", null, null },
            { "fbf3a00b", "92e85004", null, null },
            { "7a365cc2", "7d18e429", null, null },
            { "a5c31631", "952f1d85", null, null },
            { "7a365cc2", "10545ed3", "4dae70af", null },
            { "6cb74008", "cbdce7a4", null, null },
            { "19634ad2", "b193cb04", null, null },
            { "fbf3a00b", "afc30081", null, null },
            { "6cb74008", "cb81c430", null, null },
            { "fbf3a00b", "824e5e1f", null, null },
            { "6cb74008", "7faaca8c", "b7b090cf", null },
            { "19634ad2", "869bd8d8", "b7b090cf", null },
            { "fbf3a00b", "6245af61", null, null },
            { "a5c31631", "e5fae2fb", "c7af9494", null },
            { "a5c31631", "7faaca8c", null, null },
            { "fbf3a00b", "2befa75a", "b0aeb7df", null },
            { "a5c31631", "e5fae2fb", "d7e0d9e", null },
            { "6cb74008", "7faaca8c", "87cb2bff", null },
            { "6cb74008", "42b4e3a3", null, null },
            { "362aae07", "2befa75a", null, null },
            { "a5c31631", "869bd8d8", "4c4076d5", null },
            { "a5c31631", "efa22418", null, null },
            { "19634ad2", "2befa75a", "b7b090cf", null },
            { "362aae07", "7faaca8c", "59d9e201", null },
            { "7a365cc2", "235d50bf", null, null },
            { "a5c31631", "66b0fffd", null, null },
            { "6cb74008", "e3f1ef01", null, null },
            { "a5c31631", "7faaca8c", "7191ec30", null },
            { "7a365cc2", "7191ec30", null, null },
            { "7a365cc2", "2f5059f3", null, null },
            { "362aae07", "b193cb04", null, null },
            { "a5c31631", "869bd8d8", "510344a7", null },
            { "19634ad2", "ad042183", null, null },
            { "362aae07", "7faaca8c", "e6117eb6", null },
            { "362aae07", "4c1ac47d", null, null },
            { "6cb74008", "7faaca8c", "cb81c430", null },
            { "d93ccb2", "54112535", null, null },
            { "fbf3a00b", "4d0779c1", null, null },
            { "7a365cc2", "cb44703d", null, null },
            { "fbf3a00b", "5e841ec3", null, null },
            { "fbf3a00b", "335e8694", null, null },
            { "19634ad2", "7191ec30", "b7b090cf", null },
            { "a5c31631", "7faaca8c", "ab99a1a", null },
            { "362aae07", "1c9d4f11", null, null },
            { "a5c31631", "869bd8d8", "d1fb16cc", null },
            { "a5c31631", "869bd8d8", "fd1b186c", null },
            { "a5c31631", "869bd8d8", "fe4199fc", null },
            { "fbf3a00b", "2befa75a", "10545ed3", null },
            { "19634ad2", "e5fae2fb", "82610c41", "b0aeb7df" },
            { "a5c31631", "869bd8d8", null, null },
            { "7a365cc2", "f0d5fe71", null, null },
            { "19634ad2", "e5fae2fb", "2befa75a", "10545ed3" },
            { "a5c31631", "e5fae2fb", "4d0779c1", null },
            { "fbf3a00b", "387e568f", null, null },
            { "6cb74008", "62519049", null, null },
            { "6cb74008", "372ac81b", null, null },
            { "fbf3a00b", "a9e89fe4", null, null },
            { "6cb74008", "7faaca8c", "952f1d85", null },
            { "7a365cc2", "4dae70af", "a2880ff1", "427a1751" },
            { "82ff5e94", "10545ed3", null, null },
            { "7a365cc2", "b397af9f", null, null },
            { "28b33136", "7faaca8c", "7191ec30", "672fdf93" },
            { "19634ad2", "b7b090cf", null, null },
            { "de3bc3d1", "7faaca8c", "5dfed122", null },
            { "a5c31631", "10545ed3", "bddfa3f3", null },
            { "7a365cc2", "acc8c8fb", null, null },
            { "a5c31631", "e5fae2fb", "335e8694", null },
            { "19634ad2", "2f5059f3", "b7b090cf", null },
            { "fbf3a00b", "d7e0d9e", null, null },
            { "6cb74008", "afc30081", null, null },
            { "6cb74008", "eb608ee5", null, null },
            { "a5c31631", "e5fae2fb", "59d9e201", null },
            { "6cb74008", "7faaca8c", "824e5e1f", null },
            { "6cb74008", null, null, null },
            { "6cb74008", "2befa75a", "e5fae2fb", null },
            { "a5c31631", "e5fae2fb", null, null },
            { "362aae07", "7faaca8c", "4456d619", null },
            { "a5c31631", "e5fae2fb", "952f1d85", null },
            { "a5c31631", "b7b090cf", null, null },
            { "fbf3a00b", "b09cf4ef", null, null },
            { "7a365cc2", "7faaca8c", "92e85004", null },
            { "19634ad2", "fe5cf0f4", null, null },
            { "19634ad2", "9b3aa0b1", null, null },
            { "fbf3a00b", "e53a31b5", null, null },
            { "a5c31631", "e5fae2fb", "5e841ec3", null },
            { "a5c31631", "7210b66f", "f49b158d", null },
            { "6cb74008", "7faaca8c", "d6753be3", null },
            { "a5c31631", "10545ed3", "b7b090cf", null },
            { "19634ad2", "7faaca8c", "b7b090cf", null },
            { "a5c31631", "9b3aa0b1", null, null },
            { "6cb74008", "5a85e6f7", null, null },
            { "a5c31631", "869bd8d8", "5a85e6f7", null },
            { "6cb74008", "7210b66f", "f49b158d", null },
            { "82ff5e94", "ae1e4f7", null, null },
            { "fbf3a00b", "cb81c430", null, null },
            { "19634ad2", "42b4e3a3", null, null },
            { "19634ad2", "cf0f809a", null, null },
            { "fbf3a00b", "6cf35bfb", null, null },
            { "28b33136", "7faaca8c", "36a4b4a8", "82610c41" },
            { "6cb74008", "7210b66f", "5dfed122", null },
            { "a5c31631", "869bd8d8", "b7b090cf", null },
            { "7a365cc2", "7faaca8c", "335e8694", null },
            { "19634ad2", "36a4b4a8", null, null },
            { "a5c31631", "fe5cf0f4", "cbdce7a4", null },
            { "de3bc3d1", "66fbf43f", "5dfed122", null },
            { "a5c31631", "7faaca8c", "c7af9494", null },
            { "82ff5e94", "92e85004", null, null },
            { "19634ad2", "cbdce7a4", null, null },
            { "362aae07", "7191ec30", null, null },
            { "19634ad2", "b193cb04", "b7b090cf", null },
            { "19634ad2", "869bd8d8", "b193cb04", "b0aeb7df" },
            { "19634ad2", "869bd8d8", "82610c41", "2b187d87" },
            { "6cb74008", "7faaca8c", "6245af61", null },
            { "19634ad2", "bddfa3f3", null, null },
            { "6cb74008", "2f5059f3", "4b097186", null },
            { "7a365cc2", "952f1d85", null, null },
            { "a5c31631", "869bd8d8", "faa6eb0", null },
            { "6cb74008", "2befa75a", "e5fae2fb", "cf0f809a" },
            { "19634ad2", "1c9d4f11", "b7b090cf", null },
            { "7a365cc2", "b193cb04", "d1fb16cc", null },
            { "a5c31631", "869bd8d8", "1b1fba11", null },
            { "fbf3a00b", "82610c41", "510344a7", null },
            { "19634ad2", "54112535", null, null },
            { "19634ad2", "7faaca8c", "7191ec30", "b7b090cf" },
            { "19634ad2", "b193cb04", "fe5cf0f4", "5ba1ba9b" },
            { "7a365cc2", "36a4b4a8", null, null },
            { "a5c31631", "7faaca8c", "bddfa3f3", null },
            { "de3bc3d1", "7faaca8c", "2f5059f3", "c3bebfe6" },
            { "de3bc3d1", "7faaca8c", "b193cb04", "bfc8669e" },
            { "fbf3a00b", "7faaca8c", "fc65290b", null },
            { "362aae07", "6f23470b", null, null },
            { "de3bc3d1", "7faaca8c", "b193cb04", "1735a791" },
            { "a5c31631", "7faaca8c", "36a4b4a8", null },
            { "362aae07", "fd1b186c", null, null },
            { "a5c31631", "869bd8d8", "48176644", null },
            { "82ff5e94", "fe5cf0f4", null, null },
            { "82ff5e94", "9b3aa0b1", null, null },
            { "7a365cc2", "e5fae2fb", null, null },
            { "82ff5e94", "42b4e3a3", null, null },
            { "7a365cc2", "869bd8d8", null, null },
            { "19634ad2", "4dae70af", null, null },
            { "19634ad2", "2b187d87", null, null },
            { "362aae07", "7faaca8c", "18f6b23e", null },
            { "fbf3a00b", "6e9d4a77", null, null },
            { "7a365cc2", "82610c41", "b0aeb7df", null },
            { "e3621cc2", "e5fae2fb", "adafc09c", null },
            { "a5c31631", "869bd8d8", "cbdce7a4", null },
            { "7a365cc2", "7faaca8c", "6e9d4a77", null },
            { "a5c31631", "bddfa3f3", null, null },
            { "6cb74008", "e5fae2fb", null, null },
            { "d93ccb2", "2b187d87", null, null },
            { "a5c31631", "5a85e6f7", null, null },
            { "19634ad2", "d842a67d", null, null },
            { "a5c31631", "672fdf93", null, null },
            { "a5c31631", "7faaca8c", "fc65290b", null },
            { "6cb74008", "9b3aa0b1", null, null },
            { "6cb74008", "4c4076d5", null, null },
            { "7a365cc2", "7faaca8c", "6245af61", null },
            { "6cb74008", "b7b090cf", null, null },
            { "7a365cc2", "36a4b4a8", "52a6a74c", null },
            { "7a365cc2", "36a4b4a8", "a4ed278a", null },
            { "a5c31631", "7faaca8c", "b7b090cf", null },
            { "7a365cc2", "bddfa3f3", null, null },
            { "362aae07", "7faaca8c", "efa22418", null },
            { "82ff5e94", "54112535", null, null },
            { "a5c31631", "7faaca8c", "1e0083d1", null },
            { "19634ad2", "869bd8d8", "a2880ff1", "4dae70af" },
            { "362aae07", "7faaca8c", "13446f50", null },
            { "fbf3a00b", "fe5cf0f4", "36a4b4a8", "d24107f2" },
            { "fbf3a00b", "55d21172", null, null },
            { "362aae07", "7faaca8c", "1c9d4f11", null },
            { "a5c31631", "869bd8d8", "36a4b4a8", "b0aeb7df" },
            { "362aae07", "b46295d4", "672fdf93", null },
            { "82ff5e94", "387e568f", null, null },
            { "fbf3a00b", "f6bb8637", null, null },
            { "a5c31631", "e5fae2fb", "13446f50", null },
            { "362aae07", "7faaca8c", "6245af61", null },
            { "19634ad2", "2befa75a", null, null },
            { "82ff5e94", "e2f4d6e2", null, null },
        };

        private readonly IReadOnlyList<Row> _symbols;

        #region construction
        private Sigil([NotNull] IReadOnlyList<Row> value)
        {
            _symbols = value;
        }

        public Sigil(byte value)
            : this(new[] { new Row(Prefixes, value) })
        {
        }

        public Sigil(ushort value)
            : this(new[] { new Row(Prefixes, value & 0xFF), new Row(Suffixes, (value >> 8) & 0xFF) })
        {
        }

        public Sigil(uint value)
            : this(new[] { new Row(Suffixes, value & 0xff), new Row(Prefixes, (value >> 8) & 0xff), new Row(Suffixes, (value >> 16) & 0xff), new Row(Prefixes, (value >> 24) & 0xff) })
        {
        }

        public Sigil(ulong value)
            : this(new[] {
                new Row(Suffixes, value & 0xff),         new Row(Prefixes, (value >> 8) & 0xff),  new Row(Suffixes, (value >> 16) & 0xff),
                new Row(Prefixes, (value >> 24) & 0xff), new Row(Suffixes, Hash8(value)),         new Row(Prefixes, (value >> 32) & 0xff),
                new Row(Suffixes, (value >> 40) & 0xff), new Row(Prefixes, (value >> 48) & 0xff), new Row(Suffixes, (value >> 56) & 0xff)
            })
        {
        }

        public Sigil(sbyte value)
            : this(unchecked((byte)value))
        {
        }

        public Sigil(short value)
            : this(unchecked((ushort)value))
        {
        }

        public Sigil(int value)
            : this(unchecked((uint)value))
        {
        }

        public Sigil(long value)
            : this(unchecked((ulong)value))
        {
        }

        public static string SigilSvg(int value, [NotNull] string fg, [NotNull] string bg)
        {
            return new Sigil(value).ToSvg(fg, bg).ToString();
        }
        #endregion

        private static byte Hash8(ulong value)
        {
            unchecked
            {
                value *= 961764127;
            }

            return (byte)(value & 0xFF);
        }

        [NotNull] public XDocument ToSvg([NotNull] string fg = "black", [NotNull] string bg = "white")
        {
            var ns = SvgNamespace;

            //Create document and add a background
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            var root = new XElement(ns + "svg",
                new XAttribute("version", "1.1"),
                new XAttribute("baseProfile", "full"),
                new XAttribute("width", 128),
                new XAttribute("height", 128),
                new XAttribute("viewbox", "0 0 128 128")
            );
            doc.Add(root);
            root.Add(new XElement(ns + "rect", new XAttribute("width", 128), new XAttribute("height", 128), new XAttribute("fill", bg)));

            //Get elements for symbols
            var elements = new List<XElement>();
            foreach (var item in _symbols)
            {
                var g = new XElement(ns + "g");
                foreach (var id in item)
                {
                    var el = SymbolGraph.LookupId(id, bg, fg);
                    g.Add(el);
                }

                elements.Add(g);
            }

            //Layout them out on the 128 pixel grid
            var count = elements.Count;
            if (count == 2)
            {
                Layout(root, elements, 2, 2, 32);
            }
            else if (count == 1 || count == 4 || count == 9)
            {
                var dim = (int)Math.Sqrt(count);
                Layout(root, elements, dim, dim);
            }
            else
                throw new NotSupportedException($"Incorrect number of elements ({count})");

            return doc;
        }

        private static void Layout(XContainer root, IReadOnlyList<XElement> elements, int width, int height, int yoffset = 0)
        {
            var scalex = 1f / width;
            var scaley = 1f / height;

            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    var index = i + j * width;
                    if (index >= elements.Count)
                        break;

                    var el = elements[index];
                    var x = (128f / width) * i;
                    var y = (128f / height) * j + yoffset;
                    el.SetAttributeValue("transform", $"translate({x},{y}) scale({scalex},{scaley})");
                    root.Add(el);
                }
            }
        }

        private struct Row
            : IEnumerable<string>
        {
            private readonly string[,] _matrix;
            private readonly uint _row;

            public Row(string[,] matrix, int row)
                : this(matrix, unchecked((uint)row))
            {
            }

            public Row(string[,] matrix, ulong row)
                : this(matrix, unchecked((uint)row))
            {
            }

            public Row(string[,] matrix, uint row)
            {
                _matrix = matrix;
                _row = row;
            }

            public IEnumerator<string> GetEnumerator()
            {
                var width = _matrix.GetLength(1);
                for (var i = 0; i < width; i++)
                {
                    var v = _matrix[_row, i];
                    if (v == null)
                        break;
                    yield return v;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
