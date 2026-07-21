      ******************************************************************
      ** MODIFICATION LOG                                              *
      ******************************************************************
NR0625*  06/2025   NEERAR  DMND0001266 HIDE UOMS FROM GUI INTERACE     *
NR0625******************************************************************
KS0115** KSHEKA   01/21/2015   CR#14-0140 : BREAK BULK FEE CALCULATION *
KS0115**                       ADDED NEW FIELD I_ALT_UOM_DESIGNATOR FOR*
KS0115**                       DESIGNATING SELLING, BREAK BULK & LOW   *
KS0115**                       UOMS                                    *
KS0115******************************************************************
       01  VNG05.                                                       00000010
           05 VNG05-C.                                                  00000020
              10 VNG05-VN-VEND-ALT-UM.                                  00000030
                 15 VNG05-I-VENDOR                    PIC X(4)          00000040
                                           VALUE     SPACE.             00000050
                 15 VNG05-I-VND-PRODUCT               PIC X(8)          00000060
                                           VALUE     SPACE.             00000070
                 15 VNG05-C-VD-PRD-ALT-UM             PIC X(2)          00000080
                                           VALUE     SPACE.             00000090
                 15 VNG05-A-VD-PRD-ALT-UMF            PIC S9(6)V9(8)    00000100
                                           VALUE     ZERO               00000110
                                           COMPUTATIONAL-3.             00000120
KS0115           15 VNG05-F-ALT-UOM-DESIGNATOR        PIC X(1)
KS0115                                     VALUE     SPACE.
NR0625           15 VNG05-F-ALT-UNIT-OF-SALE-SW       PIC X(1)
NR0625                                     VALUE      SPACE.
NR0625           15 VNG05-F-ALT-UNIT-OF-USE-SW       PIC X(1)
NR0625                                     VALUE      SPACE.

                 EXEC SQL                                               00000130
                   DECLARE VNG05 TABLE                                  00000140
                       (I_VENDOR             CHAR(004)                  00000150
                                             NOT NULL,                  00000160
                        I_VND_PRODUCT        CHAR(008)                  00000170
                                             NOT NULL,                  00000180
                        C_VD_PRD_ALT_UM      CHAR(002)                  00000190
                                             NOT NULL,                  00000200
                        A_VD_PRD_ALT_UMF     DECIMAL(014,08)            00000210
                                             NOT NULL                   00000220
KS0115                  F_ALT_UOM_DESIGNATOR CHAR(001)
KS0115                                       NOT NULL)
NR0625                  F_ALT_UNIT_OF_SALE_SW CHAR(001)
NR0625                                       NOT NULL)
NR0625                  F_ALT_UNIT_OF_USE_SW  CHAR(001)
NR0625                                       NOT NULL)

                 END-EXEC.                                              00000230
       01  VNG05-NULL-INDICATORS.                                       00000240
              10 VNG05-NO-NULL-VARIABLES              PIC S9(4)         00000250
                                           VALUE     +0000              00000260
                                           COMPUTATIONAL.               00000270
