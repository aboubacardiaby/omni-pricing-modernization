      ******************************************************************00000010
      * DCLGEN TABLE(CU_AC_FREIGHT_FLAG)                               *00000020
      *        LIBRARY(SNARAY.TEST.COPYBK(CUG53))                      *00000030
      *        ACTION(REPLACE)                                         *00000040
      *        LANGUAGE(COBOL)                                         *00000050
      *        QUOTE                                                   *00000060
      * ... IS THE DCLGEN COMMAND THAT MADE THE FOLLOWING STATEMENTS   *00000070
      ******************************************************************00000080
           EXEC SQL DECLARE CUG53 TABLE                                 00000090
           ( I_ACCOUNT                      INTEGER NOT NULL,           00000100
             D_EFF_DATE                     DATE NOT NULL,              00000110
             D_EXP_DATE                     DATE,                       00000120
             F_FRT_FLAG                     CHAR(1) NOT NULL            00000130
SP0411       F_EXEMPT_SANC_FLAG             CHAR(1) NOT NULL
SP0411       F_EXEMPT_NON_SANC_FLAG         CHAR(1) NOT NULL
SP0411       F_EXEMPT_IND_FLAG              CHAR(1) NOT NULL
SP0411       F_EXEMPT_NON_CONT_FLAG         CHAR(1) NOT NULL
SP0411       F_EXEMPT_CUSTOM_FLAG           CHAR(1) NOT NULL
TA0612       I_BUY_GROUP                    INTEGER NOT NULL
           ) END-EXEC.                                                  00000140
      ******************************************************************00000150
      * COBOL DECLARATION FOR TABLE U1.CU_AC_FREIGHT_FLAG              *00000160
      ******************************************************************00000170
       01  CUG53.                                                       00000180
         05 CUG53-C.                                                    00000190
           10 CUG53-I-ACCOUNT            PIC S9(9) USAGE COMP.          00000200
           10 CUG53-D-EFF-DATE           PIC X(10).                     00000210
           10 CUG53-D-EXP-DATE           PIC X(10).                     00000220
           10 CUG53-F-FRT-FLAG           PIC X(1).                      00000230
SP0411     10 CUG53-F-GRP-SANC           PIC X(1).
SP0411     10 CUG53-F-GRP-NON-SANC       PIC X(1).
SP0411     10 CUG53-F-IND                PIC X(1).
SP0411     10 CUG53-F-NON-CONT           PIC X(1).
SP0411     10 CUG53-F-CUSTOM             PIC X(1).
TA0612     10 CUG53-I-BUY-GROUP          PIC S9(8)
TA0612                                   VALUE   ZERO  COMPUTATIONAL.
      ******************************************************************00000240
      * THE NUMBER OF COLUMNS DESCRIBED BY THIS DECLARATION IS 4       *00000250
      ******************************************************************00000260
         01  CUG53-NULL-INDICATORS.                                     00000270
          05 CUG53-CU-AC-FREIGHT-FLAG-NI.                               00000280
             10 CUG53-D-EXP-DATE-NI      PIC S9(4)                      00000290
                                 VALUE     ZERO  COMPUTATIONAL.         00000300
