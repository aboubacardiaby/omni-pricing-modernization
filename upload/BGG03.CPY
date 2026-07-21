015370 01  BGG03.                                                       00000001
015380     02 BGG03-C.                                                  00000002
015390        10 BGG03-BG-BUY-GROUP-PART.                               00000003
015400           15 BGG03-CAN-BE-THE-JUNIOR-P.                          00000004
015410              20 BGG03-I-BUY-GROUP              PIC S9(8)         00000005
015420                                     VALUE     ZERO               00000006
015430                                     COMPUTATIONAL.               00000007
015440           15 BGG03-CAN-BE-THE-SENIOR-P.                          00000008
015450              20 BGG03-I-BUY-GROUP-PARENT       PIC S9(8)         00000009
015460                                     VALUE     ZERO               00000010
015470                                     COMPUTATIONAL.               00000011
015480           15 BGG03-D-PARTNER-EXPIRE            PIC X(10)         00000012
015490                                     VALUE     SPACE.             00000013
015500           15 BGG03-D-PARTNER-EXPIRE-FULL                         00000014
015510                                     REDEFINES                    00000015
015520              BGG03-D-PARTNER-EXPIRE.                             00000016
015530           16 BGG03-D-PARTNER-EXPIRE-MO-X.                        00000017
015540           17 BGG03-D-PARTNER-EXPIRE-MO         PIC 9(2).         00000018
015550           16 BGG03-D-PARTNER-EXPIRE-S1         PIC X(1).         00000019
015560           16 BGG03-D-PARTNER-EXPIRE-DY-X.                        00000020
015570           17 BGG03-D-PARTNER-EXPIRE-DY         PIC 9(2).         00000021
015580           16 BGG03-D-PARTNER-EXPIRE-S2         PIC X(1).         00000022
015590           16 BGG03-D-PARTNER-EXPIRE-YR-X.                        00000023
015600           17 BGG03-D-PARTNER-EXPIRE-YR         PIC 9(4).         00000024
015610           15 BGG03-D-PARTNER-EFFECT            PIC X(10)         00000025
015620                                     VALUE     SPACE.             00000026
015630           15 BGG03-D-PARTNER-EFFECT-FULL                         00000027
015640                                     REDEFINES                    00000028
015650              BGG03-D-PARTNER-EFFECT.                             00000029
015660           16 BGG03-D-PARTNER-EFFECT-MO-X.                        00000030
015670           17 BGG03-D-PARTNER-EFFECT-MO         PIC 9(2).         00000031
015680           16 BGG03-D-PARTNER-EFFECT-S1         PIC X(1).         00000032
015690           16 BGG03-D-PARTNER-EFFECT-DY-X.                        00000033
015700           17 BGG03-D-PARTNER-EFFECT-DY         PIC 9(2).         00000034
015710           16 BGG03-D-PARTNER-EFFECT-S2         PIC X(1).         00000035
015720           16 BGG03-D-PARTNER-EFFECT-YR-X.                        00000036
015730           17 BGG03-D-PARTNER-EFFECT-YR         PIC 9(4).         00000037
015740           15 BGG03-D-PARTNER-START             PIC X(10)         00000038
015750                                     VALUE     SPACE.             00000039
015760           15 BGG03-D-PARTNER-START-FULL                          00000040
015770                                     REDEFINES                    00000041
015780              BGG03-D-PARTNER-START.                              00000042
015790           16 BGG03-D-PARTNER-START-MO-X.                         00000043
015800           17 BGG03-D-PARTNER-START-MO          PIC 9(2).         00000044
015810           16 BGG03-D-PARTNER-START-S1          PIC X(1).         00000045
015820           16 BGG03-D-PARTNER-START-DY-X.                         00000046
015830           17 BGG03-D-PARTNER-START-DY          PIC 9(2).         00000047
015840           16 BGG03-D-PARTNER-START-S2          PIC X(1).         00000048
015850           16 BGG03-D-PARTNER-START-YR-X.                         00000049
015860           17 BGG03-D-PARTNER-START-YR          PIC 9(4).         00000050
015870           EXEC SQL                                               00000051
015880             DECLARE BGG03 TABLE                                  00000052
015890                 (I_BUY_GROUP          INTEGER                    00000053
015900                                       ,                          00000054
015910                  I_BUY_GROUP_PARENT   INTEGER                    00000055
015920                                       NOT NULL,                  00000056
015930                  D_PARTNER_EXPIRE     DATE                       00000057
015940                                       ,                          00000058
015950                  D_PARTNER_EFFECT     DATE                       00000059
015960                                       NOT NULL,                  00000060
015970                  D_PARTNER_START      DATE                       00000061
015980                                       NOT NULL)                  00000062
015990           END-EXEC.                                              00000063
016000 01  BGG03-NULL-INDICATORS.                                       00000064
016010     05 BGG03-CAN-BE-THE-JUNIOR-P-NI.                             00000065
016020        10 BGG03-I-BUY-GROUP-NI                 PIC S9(4)         00000066
016030                                     VALUE     ZERO               00000067
016040                                     COMPUTATIONAL.               00000068
016050        10 BGG03-D-PARTNER-EXPIRE-NI            PIC S9(4)         00000069
016060                                     VALUE     ZERO               00000070
016070                                     COMPUTATIONAL.               00000071
016080        10 BGG03-NO-NULL-VARIABLES              PIC S9(4)         00000072
016090                                     VALUE     +0002              00000073
016100                                     COMPUTATIONAL.               00000074
