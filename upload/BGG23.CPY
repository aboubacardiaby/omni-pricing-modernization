      ******************************************************************00000010
      * DCLGEN TABLE(P1.BG_LOW_UOM)                                    *00000020
      *        CREATE BY JOE GIGLIO                                     00000030
      *        LANGUAGE(COBOL)                                         *00000040
      *        QUOTE                                                   *00000050
      * ... IS THE DCLGEN COMMAND THAT MADE THE FOLLOWING STATEMENTS   *00000060
      * COBOL DECLARATION FOR TABLE P1.BG_LOW_UOM                      *00000070
      ******************************************************************00000080
017540 01  BGG23.                                                       00000090
017541     02 BGG23-C.                                                  00000100
017542        10 BGG23-BG-BUY-GROUP.                                    00000110
017543           15 BGG23-I-BUY-GROUP                 PIC S9(8)         00000120
017544                                     VALUE     ZERO               00000130
017545                                     COMPUTATIONAL.               00000140
017546           15 BGG23-D-BG-LOW-UOM-EFF            PIC X(10)         00000150
017547                                     VALUE     SPACE.             00000160
017548           15 BGG23-D-BG-LOW-UOM-EFF-FULL                         00000170
017549                                     REDEFINES                    00000180
017550              BGG23-D-BG-LOW-UOM-EFF.                             00000190
017551           16 BGG23-D-BG-LOW-UOM-EFF-MO-X.                        00000200
017552           17 BGG23-D-BG-LOW-UOM-EFF-MO         PIC 9(2).         00000210
017553           16 BGG23-D-BG-LOW-UOM-EFF-S1         PIC X(1).         00000220
017554           16 BGG23-D-BG-LOW-UOM-EFF-DY-X.                        00000230
017555           17 BGG23-D-BG-LOW-UOM-EFF-DY         PIC 9(2).         00000240
017556           16 BGG23-D-BG-LOW-UOM-EFF-S2         PIC X(1).         00000250
017557           16 BGG23-D-BG-LOW-UOM-EFF-YR-X.                        00000260
017558           17 BGG23-D-BG-LOW-UOM-EFF-YR         PIC 9(4).         00000270
017560           15 BGG23-D-BG-LOW-UOM-EXP            PIC X(10)         00000280
017561                                     VALUE     SPACE.             00000290
017562           15 BGG23-D-BG-LOW-UOM-EXP-FULL                         00000300
017563                                     REDEFINES                    00000310
017564              BGG23-D-BG-LOW-UOM-EXP.                             00000320
017565           16 BGG23-D-BG-LOW-UOM-EXP-MO-X.                        00000330
017566           17 BGG23-D-BG-LOW-UOM-EXP-MO         PIC 9(2).         00000340
017567           16 BGG23-D-BG-LOW-UOM-EXP-S1         PIC X(1).         00000350
017568           16 BGG23-D-BG-LOW-UOM-EXP-DY-X.                        00000360
017569           17 BGG23-D-BG-LOW-UOM-EXP-DY         PIC 9(2).         00000370
017570           16 BGG23-D-BG-LOW-UOM-EXP-S2         PIC X(1).         00000380
017571           16 BGG23-D-BG-LOW-UOM-EXP-YR-X.                        00000390
017572           17 BGG23-D-BG-LOW-UOM-EXP-YR         PIC 9(4).         00000400
017573           15 BGG23-P-BG-LOW-UOM     PIC S9(1)V9(4) USAGE COMP-3. 00000410
HR0415           15 BGG23-P-BG-BRK-BULK    PIC S9(1)V9(4) USAGE COMP-3.
017575           15 BGG23-F-BG-LOW-UOM-EXCL           PIC X(01)         00000420
017576                                     VALUE     SPACE.             00000430
HR0415*          15 BGG23-C-LUOM-EXCL                 PIC X(03)         00000431
HR0415*                                    VALUE     SPACE.             00000432
017575           15 BGG23-F-LUOM-VEND-EXCL            PIC X(01)         00000433
017576                                     VALUE     SPACE.             00000434
      ******************************************************************00000440
      * THE NUMBER OF COLUMNS DESCRIBED BY THIS DECLARATION IS 5       *00000450
      ******************************************************************00000460
017577           EXEC SQL                                               00000470
017578             DECLARE BGG23 TABLE                                  00000480
017579                 (I_BUY_GROUP          INTEGER                    00000490
017580                                       NOT NULL,                  00000500
017581                  D_BG_LOW_UOM_EFF     DATE                       00000510
017582                                       NOT NULL,                  00000520
017583                  D_BG_LOW_UOM_EXP     DATE                       00000530
017584                                       ,                          00000540
017585                  P_BG_LOW_UOM         DECIMAL(5, 4)              00000550
017586                                       NOT NULL,                  00000560
HR0415                  P_BG_BREAK_BULK      DECIMAL(5, 4)
HR0415                                       NOT NULL,
017587                  F_BG_LOW_UOM_EXCL    CHAR(001)                  00000570
017588                                       NOT NULL,                  00000571
HR0415*                 C_LUOM_EXCL          CHAR(003)                  00000572
HR0415*                                      NOT NULL,                  00000573
017587                  F_LUOM_VEND_EXCL     CHAR(001)                  00000574
017588                                       NOT NULL                   00000580
                        )                                               00000590
017590           END-EXEC.                                              00000600
017591 01  BGG23-NULL-INDICATORS.                                       00000610
017592     05 BGG23-BG-BUY-GROUP-NI.                                    00000620
017593        10 BGG23-D-BG-LOW-UOM-EXP-NI            PIC S9(4)         00000630
017594                                     VALUE     ZERO               00000640
017595                                     COMPUTATIONAL.               00000650
017596        10 BGG23-NO-NULL-VARIABLES              PIC S9(4)         00000660
017597                                     VALUE     +0001              00000670
017598                                     COMPUTATIONAL.               00000680
