/*
 * InternalKeys.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if INTERNALS_VISIBLE_TO
using Eagle._Attributes;

namespace Eagle._Components.Private
{
    [ObjectId("e8ea69c1-5c7f-432c-b00f-1a1076d57cbb")]
    internal sealed class InternalKeys
    {
        //
        // NOTE: Debug builds (public and private) of the Eagle core library MAY
        //       be signed with this public key ("EagleFastPublic.snk", 4096
        //       bits).  These are really only included inline here for use with
        //       the InternalsVisibleTo attributes (i.e. for some reason they
        //       cannot simply use the publicKeyToken) used in this assembly.
        //
        //       This key MAY also be used to sign release builds.
        //
        public const string Fast =
            "0024000004800000140200000602000000240000525341310010000001000100" +
            "1f39c3a9757db25d7202147995d354f422ed1cd4999366a5192ee7088513a634" +
            "43577caca4dd1ae625f351254ffa36576a4df523e3c9e30146d8415d1aea8e76" +
            "a28ac7e53401410bbfa52dd72e7bd6906228f25029241b2a28fc187bb2e0f563" +
            "343b28c9754df6645f32e7759191e6b31b6569964210a8fd3078a794a6dee4da" +
            "68c8d7ddbbcb73dcb7ec8f496d2c105a9c32749bdb5335f164d50f3946b0c59c" +
            "2ef029549d5c7457f6b97b47703a0cac514e9216b778d8fee1ddca740ab26631" +
            "4ea2c7196d9c163c4bd0deaf20d5c8a852592ed3c8c72938c52e95ec988c5d8c" +
            "b58683af3f28c780c12983841e18142f4fc2b8b91c97f4573655e2e34b10504a" +
            "077c55767e3c4e8d03c50f4866a4cb7ec0eb47229084addab34b8554d4705a77" +
            "1867c479e0f11a68f1ebe9978996474889ad8d4f1b5c1851858f6efddf26e9dd" +
            "af8c276e2c850a2324b3a06b455e82a57e28440eb605d200efc2ca63e3d23134" +
            "0e6a835a1ea88b202d8255d65be314aff3e5c2402a8bfde08995c4170fc1a6fb" +
            "3140d186356e4d7f62f6aa9f01e7ff9dea9d2d5b59d9e4aca95c54eea0ccd271" +
            "d4e81141384d0b17a489ea66134e526665ea330bdfacb8edaeed931f35983351" +
            "7218ba937d2417d03d5f38c26c5b5d313b04d984350cb291ffed23b40a08ccf2" +
            "52af9512d2c88149b64718ff1a522a51563981d36cb143239944bf88a89a51a7";

        //
        // NOTE: Public release builds of the Eagle core library MAY be signed
        //       with this public key ("EagleStrongPublic.snk", 16384 bits).
        //       These are really only included inline here for use with the
        //       InternalsVisibleTo attributes (i.e. for some reason they cannot
        //       simply use the publicKeyToken) used in this assembly.
        //
        //       This key MUST NOT be used to sign debug builds.
        //
        public const string Strong =
            "0024000004800000140800000602000000240000525341310040000001000100" +
            "a355d4347022d498c75042ae648cd20f6e932880203d41fe47046d97494516b6" +
            "f968b5ccacaa2c1ea2ee0ffebe44c9ff15fefb5eeb5d6d22b9a4638fba333d6b" +
            "baf7fc058184b15c7f28af31ace5a9e444cce456ea7048617a54896784011a1f" +
            "d0d6eda68b4bb28459a04645367625ce78973d5537350a3e59429efced08ac1d" +
            "e701235804d67d4f229c5928a661d7023829efb3a34918105a8ccd62840413fc" +
            "bef376b8630877695f5121199c53c70dcbefd3f7386371d8b09c6eba1e072ce8" +
            "cd66a8b1d50d9d8f9c4f2a18912bbe07f100da68ddb1b1044ed920331096acc2" +
            "d9e394de814724f2539e1e5db7d458734c9383727434767bdeb241ebda398f23" +
            "d6a1c599a16ecaac47e466b72a35da07f50c865316484d206b32f0da0ecf5584" +
            "78e2e0ab925b6f37a954abfc8fa2df2c83f26c87e7276c717dc02de06d98bbb1" +
            "3b95423fc3d4f5ab5aa985b076bdf2b0908c7aab6f4d50011fbe46559c2da87a" +
            "e508ba15f8a3b73e85d150176725622d486ef566b7da90f09839acf7fbe91a0d" +
            "25b7beec12802ddf1f2561a3f966f4890ba0878109b4eb6189d97f7e8c6325f0" +
            "5bd5c4720a50e4c90319eb2e2301400546974c45d50cf54d28365fba9f563120" +
            "7509c9fb1b1568040d87b16423240fb8eabc2ba222d7272848ad4fbf37fa0a48" +
            "fd46855157682b15a0ef28ca851fda76d3a646f5daaccf63a12aff07a306b369" +
            "0ae10faf12d6b6d239d208f6e9a9533000cc3031dc6c688c0b085211a1880a1d" +
            "431980af69690679f727d3d236da8412992e5acc1dae4a5e997b46c43341e13f" +
            "cfa3b97ae48d48624c081d581b8d698190aa3ad16fabdbecaf8f00b2bad9e993" +
            "2e17714c9a9b044cd87400830b491a066fa1384f920234329af907b3b012f494" +
            "010a83e81ee2b7dcad8d14fbf87cf33a5160b32508ed5766c14f2259b2c36def" +
            "e0dc68f1bc2d1927faa13720ce03da5845411c80dd2d38ce6cb5fde97c516bc0" +
            "1882c03466cbc555947e44834c708cab38c9c88df7203145928742a4b510ddab" +
            "b3e3b7145066ba44af476ea917b2f1f5220bf3d0497f3d62f3b5f2668a214d20" +
            "c55b686e54431d831171ab36dfec040e5101987c74badd56854d366bb980f855" +
            "c332be618a2f376d5c4783af8b7dc8a6995a9db02ad63c4ddd70da2c8ee1ea19" +
            "6a83c855cb1c51ea1f5ac34616581f227c160ca48679f6e86d2230e2f28e32a1" +
            "310a7b43a796d41f91f2eefb4969cd7e0389e6d1e8b2f09b246bbbe17256ab0a" +
            "411e571d8f77df23bec40f950472aefba93977945a0b2995c8da0b2d178145f9" +
            "72a027b2a1d437ddc38647ad524b4497973ceda7a49d49c60b77433018aadced" +
            "c74a9af9c961285b896e6d8bc40a635e002cb6329fb9f99bbd0cd90dbd813b9a" +
            "d1b8594bd8b5b3a543a67a142f2499da403ffd7f409957db0c81ec6843793cb3" +
            "a14671ae58ace1652a0c94760c73cd533a1e205da4409dbc80b701c3e6446630" +
            "422fae3ceaaaf3b53a04f295bbcc9472b77bac4469dc322af56696aeade317de" +
            "d8e0b85ba89be099e1870bf2d4e6d54d4820eae60069bc1615d5f9c62fa51611" +
            "02144ccffca7a9d5d4e7fec3851ee1af011a439603330e0bbb6cc2406bd4301d" +
            "895e125c6260d1657da4da69da5b7367c37c74550fac8274377c53b213973cad" +
            "4291e455f220ca2ac35b559779bf1d03faf5f4b80d28106701f31be620d9d99c" +
            "548262b9ac200165d12bdbd58fa5c98316a47f0bcfc0a2b211bc9bccbc12191a" +
            "73a430fdf0b76bb6054c057504579bcedd6bb062d5225de4f30b2b9bf9efa583" +
            "b6e0e6fbb37c45f9369dddb531cf47b47b8107709a2d7fc180c6fb88fe2870bb" +
            "3e03b41dbac9e6252f050b0aad7d689f3298ca2f9ad2d2b3c255ae4886af1b88" +
            "f8cbe9a4f8affc6b8700496a0388735e5fe790ce6a914f46e02ec1e9fc3a89de" +
            "da3a77b51367985012a8b6a28f9e85f84b34a958c19546d15d8e82cd56a035c9" +
            "b10003319267b64be68420e82f5caac1ebcf88691f8495afa49907816df1bef8" +
            "314dbd825c5e1228d61a21a432b75e26bad7360f6252805eb6eaf824676ca19c" +
            "890d2217eabb34b70b7143983548b5489391180c01180f2f766781428413d24e" +
            "b26f2412d17aee6cbdd22cb70b18598727734b64d472fbde49285dc79c2b49cd" +
            "7797d5939dabf0aff4f9a9fed4b8d9a0d565844de0171c914e913365310ec698" +
            "4a6c018e183bb2b72b28d007a6fa4787a207c10340ed06d869e7e34285ac5f5e" +
            "7f4aeb49cd178a81901585842a361cc1c191a2fe887af0365c0836136cfb6d22" +
            "a7c9c661e84a4c2e0d2243b3212153776813462cf4bd48b623839e137143ab72" +
            "3264a395de1be077c04c230a7e0ae7dc0bbc5bde197ae00bb3aa8583b4f2fb00" +
            "0c5f99eb5c2d5b6c5e76a22049766e235e15d81c1b2576d238c83dadaebb5c39" +
            "b9ecba4947e70e70e1cf854a25f0b7ff79fe96c37533ccf3d3b837dcdf5b36bb" +
            "5b39b75c4603d10251ee35fb8da3c9b7014b180d9bddb557efead435fb448ae5" +
            "510471cc8e1c9ed0242b4c4dc00959ca5e90a8b8adbc67b490c1406ed0eacfab" +
            "97a34eee0b549c876934e2565287ed7beb2ccf9168091cf3a5ea1fd60f84791f" +
            "6db20f0541ffd9b3861179557f7a7aac8eba9e9ddc2ccf78ba6b7f8c87b2acae" +
            "27ad798c3336d72726dae5750db426577d9ab7cf5ac1edce71dcdb8566e08281" +
            "bbb88906dbbec8f00ef9add70ff74dcd9f5563e09cc7a23d7a7035dfc473539d" +
            "26a3f94b821d2db8859c0e88259fa1cc1f4dfbddf6869d767de17ca937e5827b" +
            "f3e13439923574a74ef1051c0463aa69a2cef340286d225551516b4c27967a79" +
            "98afbe03cf283021d9794281b64e4f5c5faedbc2a190c9b8b512dd200709e6c1";

        //
        // NOTE: Public pre-release builds of the Eagle core library MAY be
        //       signed with this public key ("EagleBetaPublic.snk", 8200 bits).
        //       These are really only included inline here for use with the
        //       InternalsVisibleTo attributes (i.e. for some reason they cannot
        //       simply use the publicKeyToken) used in this assembly.
        //
        //       This key MUST NOT be used to sign actual release builds.
        //
        public const string Beta =
            "0024000004800000150400000602000000240000525341310820000001000100" +
            "4bf91bb3d80e40787ff2f12c8085af4c472c0004a55bfbb1826384d734353120" +
            "4784f6bf0ecd13abb9a69c7ee7453d7db2d5511b159d0ed79ae1cf6ac1e7de6f" +
            "ae967ca664b916fcc063713cb6e358ad74fa34d49a9a2ef0c895e9e9eae883d3" +
            "e9684c95a32569ea3cb480ef332f1bd01afa04f5916f9394ec03411518f40237" +
            "51b047fcd77b98536063e646680d0969fb97c90238a3933806dc3a6bb7d6d824" +
            "09990b609a46fe3819b4ce4ef46352ffbe64ea2871c403d54bc639252bf4c116" +
            "2b35e611f7d326e03c88af03901c23772dcd0981b664c5636e6492cb62c39b74" +
            "c16f3f6949e6abc9d297b2cf5bdaea9ebdcc658f0f97b0d799c79f18c667cee0" +
            "1090732f3f40b249bf100a8d088a6c096e0b8baabcb4c15cbbf1d7e1ef559bb5" +
            "ad2a213fffdb457f70cd9955add9eee38a0799ce8fa1aa1745ef71cd9ea1fecf" +
            "1dfe1236a1479d61f0847742ba423bdd5ca1367bc7550a6e74afceeab26a82e5" +
            "8a905abe749cc30dad8b15b86767ec054fbe9c7488b359675cf66eae61c522c1" +
            "37e288813c27791502e77bbc78219a32b10f5911921da63cc037a738453236f7" +
            "b0fe1ae44b91172855becc86ae0d09a60267aa9e90e722827f8d67e43d6c6215" +
            "bbb27348649b111152948f438f17f81e8c8b54141e8e778b18854bf26b5ae33d" +
            "b69d03d1aed6cc31e87f684bee87efba8ca784dd1cad6724dbc38f083df0e7fd" +
            "05adece9fd1fc73160223cb52331d3d806d526056570c1e14343b9bfe84177d3" +
            "9d80d2e742163ad92c45fd39ffc4c045e83900200a4757ab0560edbad12ee9b7" +
            "9259d82c1fe002c7e7fb01a3c89ff5cb0c845c62fc36fb15b1a2e6244210b9ab" +
            "a558777157cbcf6d3ca3d338097c0955fffc142a7031202097c4938341863e98" +
            "55f0423f90c042c6ee39dd99dd03a27d8116a705a4b91286c151ff92ab28d274" +
            "418a625e8ee857d1ffb4af0794d665d8b51b2e4f873b6bbf80bb584c03c69292" +
            "3ba1e1159bc89160cd32fa31e9b81cd2e513c6ad78929d07bce56b7986b14643" +
            "957339fd4551fee6ffbd9748e03200e5423c7c0f3517d43a3c2ddf0691e504c1" +
            "4274b0cf70aaf6509bb47793f8d49afcadefad2eb50b0aa028f5bed633bbd249" +
            "8887c97a2dc5ed236191a47b5afe68c6ae39b69836368eb6587f24761ccdcd3d" +
            "e17f155aa92f02bf91516eee50b26b773cb15f25f426c8817448e16179533387" +
            "a34d2e910c78fbfec430d184e22e72239adeaa4e91a9b09f3ec2359e850b1047" +
            "301588e3b5761591af6be7e24fc2f2d624f954c2f874614fcff9d83ff5bf6188" +
            "e86f926236408b67e507180516411055250395c9186dab96945c8ccfa4746b6d" +
            "10cb56e17eb012c82420ed474910d7fa518f56eab8c2afd8dc3aeb5d3f7e3b22" +
            "c51ea4174280f6e0132f98460243f53daf78b31ff215147cbe5f7be9eb01f3ef" +
            "a6";
    }
}
#endif
