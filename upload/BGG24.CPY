000100******************************************************************00000010
000200* DCLGEN TABLE(P1.BG_LOW_UOM_EXCL)                               *00000020
000300*        CREATED BY JOE GIGLIO                                    00000030
000400*        LANGUAGE(COBOL)                                         *00000040
000500*        QUOTE                                                   *00000050
000600* ... IS THE DCLGEN COMMAND THAT MADE THE FOLLOWING STATEMENTS   *00000060
000700* COBOL DECLARATION FOR TABLE P1.BG_LOW_UOM_EXCL                 *00000070
000800******************************************************************00000080
000900 01  BGG24.                                                       00000090
001000     02 BGG24-C.                                                  00000100
001100        10 BGG24-BG-BUY-GROUP.                                    00000110
001200           15 BGG24-I-BUY-GROUP                 PIC S9(8)         00000120
001300                                     VALUE     ZERO               00000130
001400                                     COMPUTATIONAL.               00000140
001500           15 BGG24-D-BG-LOW-UOM-EFF            PIC X(10)         00000150
001600                                     VALUE     SPACE.             00000160
001700           15 BGG24-D-BG-LOW-UOM-EFF-FULL                         00000170
001800                                     REDEFINES                    00000180
001900              BGG24-D-BG-LOW-UOM-EFF.                             00000190
002000           16 BGG24-D-BG-LOW-UOM-EFF-MO-X.                        00000200
002100           17 BGG24-D-BG-LOW-UOM-EFF-MO         PIC 9(2).         00000210
002200           16 BGG24-D-BG-LOW-UOM-EFF-S1         PIC X(1).         00000220
002300           16 BGG24-D-BG-LOW-UOM-EFF-DY-X.                        00000230
002400           17 BGG24-D-BG-LOW-UOM-EFF-DY         PIC 9(2).         00000240
002500           16 BGG24-D-BG-LOW-UOM-EFF-S2         PIC X(1).         00000250
002600           16 BGG24-D-BG-LOW-UOM-EFF-YR-X.                        00000260
002700           17 BGG24-D-BG-LOW-UOM-EFF-YR         PIC 9(4).         00000270
002800                                                                  00000280
002900           15 BGG24-I-CUSTOMER                  PIC S9(8)         00000290
003000                                     VALUE     ZERO               00000300
003100                                     COMPUTATIONAL.               00000310
003200           15 BGG24-D-BG-LUOM-EXCL-EFF           PIC X(10)        00000320
003300                                     VALUE     SPACE.             00000330
003400           15 BGG24-D-BG-LUOM-EXCL-EFF-FULL                       00000340
003500                                     REDEFINES                    00000350
003600              BGG24-D-BG-LUOM-EXCL-EFF.                           00000360
003700           16 BGG24-D-BG-LUOM-EXCL-EFF-MO-X.                      00000370
003800           17 BGG24-D-BG-LUOM-EXCL-EFF-MO      PIC 9(2).          00000380
003900           16 BGG24-D-BG-LUOM-EXCL-EFF-S1      PIC X(1).          00000390
004000           16 BGG24-D-BG-LUOM-EXCL-EFF-DY-X.                      00000400
004100           17 BGG24-D-BG-LUOM-EXCL-EFF-DY      PIC 9(2).          00000410
004200           16 BGG24-D-BG-LUOM-EXCL-EFF-S2      PIC X(1).          00000420
004300           16 BGG24-D-BG-LUOM-EXCL-EFF-YR-X.                      00000430
004400           17 BGG24-D-BG-LUOM-EXCL-EFF-YR      PIC 9(4).          00000440
004500                                                                  00000450
004600           15 BGG24-D-BG-LUOM-EXCL-EXP         PIC X(10)          00000460
004700                                     VALUE     SPACE.             00000470
004800           15 BGG24-D-BG-LUOM-EXCL-EXP-FULL                       00000480
004900                                     REDEFINES                    00000490
005000              BGG24-D-BG-LUOM-EXCL-EXP.                           00000500
005100           16 BGG24-D-BG-LUOM-EXCL-EXP-MO-X.                      00000510
005200           17 BGG24-D-BG-LUOM-EXCL-EXP-MO       PIC 9(2).         00000520
005300           16 BGG24-D-BG-LUOM-EXCL-EXP-S1       PIC X(1).         00000530
005400           16 BGG24-D-BG-LUOM-EXCL-EXP-DY-X.                      00000540
005500           17 BGG24-D-BG-LUOM-EXCL-EXP-DY       PIC 9(2).         00000550
005600           16 BGG24-D-BG-LUOM-EXCL-EXP-S2       PIC X(1).         00000560
005700           16 BGG24-D-BG-LUOM-EXCL-EXP-YR-X.                      00000570
005800           17 BGG24-D-BG-LUOM-EXCL-EXP-YR       PIC 9(4).         00000580
005900******************************************************************00000590
006000* THE NUMBER OF COLUMNS DESCRIBED BY THIS DECLARATION IS 5       *00000600
006100******************************************************************00000610
006200           EXEC SQL                                               00000620
006300             DECLARE BGG24 TABLE                                  00000630
006400                 (I_BUY_GROUP          INTEGER                    00000640
006500                                       NOT NULL,                  00000650
006600                  D_BG_LOW_UOM_EFF     DATE                       00000660
006700                                       NOT NULL,                  00000670
006800                  I_CUSTOMER           INTEGER                    00000680
006900                                       NOT NULL,                  00000690
007200                  D_BG_LUOM_EXCL_EFF   DATE                       00000700
007300                                       NOT NULL,                  00000710
007000                  D_BG_LUOM_EXCL_EXP   DATE                       00000720
007400                  )                                               00000730
007500           END-EXEC.                                              00000740
007600 01  BGG24-NULL-INDICATORS.                                       00000750
007700     05 BGG24-BG-BUY-GROUP-NI.                                    00000760
007800        10 BGG24-D-BG-LUOM-EXCL-EXP-NI          PIC S9(4)         00000770
007900                                     VALUE     ZERO               00000780
008000                                     COMPUTATIONAL.               00000790
008100        10 BGG24-NO-NULL-VARIABLES              PIC S9(4)         00000800
008200                                     VALUE     +0001              00000810
008300                                     COMPUTATIONAL.               00000820
