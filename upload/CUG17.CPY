000010 01  CUG17.                                                       00000010
000040     05 CUG17-C.                                                  00000020
000050        10 CUG17-CU-SHIP-TO.                                      00000030
000060           15 CUG17-F-ST-PANDAC                 PIC X(1)          00000040
000070                                     VALUE     SPACE.             00000050
000080           15 CUG17-F-ST-JIT-PART-SUB           PIC X(1)          00000060
000090                                     VALUE     SPACE.             00000070
000100           15 CUG17-C-ST-JIT-BREAK-BLK          PIC X(1)          00000080
000110                                     VALUE     SPACE.             00000090
000120           15 CUG17-C-ST-JIT-APPLY-LBL          PIC X(1)          00000100
000130                                     VALUE     SPACE.             00000110
000140           15 CUG17-C-ST-JIT-LABEL-FMT          PIC X(2)          00000120
000150                                     VALUE     SPACE.             00000130
000160           15 CUG17-C-ST-JIT-LABELS             PIC X(1)          00000140
000170                                     VALUE     SPACE.             00000150
000180           15 CUG17-F-ST-JIT-CUSTOMER           PIC X(1)          00000160
000190                                     VALUE     SPACE.             00000170
000200           15 CUG17-I-ACCOUNT                   PIC S9(8)         00000180
000210                                     VALUE     ZERO               00000190
000220                                     COMPUTATIONAL.               00000200
000230           15 CUG17-C-SHIP-TO-SUFFIX            PIC X(3)          00000210
000240                                     VALUE     SPACE.             00000220
000310           EXEC SQL                                               00000230
000320             DECLARE CUG17 TABLE                                  00000240
000330                 (F_ST_PANDAC          CHAR(001)                  00000250
000340                                       NOT NULL,                  00000260
000350                  F_ST_JIT_PART_SUB    CHAR(001)                  00000270
000360                                       NOT NULL,                  00000280
000370                  C_ST_JIT_BREAK_BLK   CHAR(001)                  00000290
000380                                       NOT NULL,                  00000300
000390                  C_ST_JIT_APPLY_LBL   CHAR(001)                  00000310
000400                                       NOT NULL,                  00000320
000410                  C_ST_JIT_LABEL_FMT   CHAR(002)                  00000330
000420                                       NOT NULL,                  00000340
000430                  C_ST_JIT_LABELS      CHAR(001)                  00000350
000440                                       NOT NULL,                  00000360
000450                  F_ST_JIT_CUSTOMER    CHAR(001)                  00000370
000460                                       NOT NULL,                  00000380
000470                  I_ACCOUNT            INTEGER                    00000390
000480                                       NOT NULL,                  00000400
000490                  C_SHIP_TO_SUFFIX     CHAR(003)                  00000410
000500                                       NOT NULL)                  00000420
000510           END-EXEC.                                              00000430
000520 01  CUG17-NULL-INDICATORS.                                       00000440
000550        10 CUG17-NO-NULL-VARIABLES              PIC S9(4)         00000450
000560                                     VALUE     +0000              00000460
000570                                     COMPUTATIONAL.               00000470
