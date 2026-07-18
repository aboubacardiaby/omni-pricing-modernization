       IDENTIFICATION DIVISION.
       PROGRAM-ID.                     A6O011U IS INITIAL.
       AUTHOR.                         SATHYA.
      ************************  REMARKS  ***************************
      *    * COMPILE = COBOL2-CICS-DB2                            *
      *    * TYPE    = 3                                          *
      **----------------------------------------------------------**
      **  NOT A DUAL COMPILER, THOUGH NAME SOUNDS LIKE ONE        **
      **----------------------------------------------------------**
      **  PROJECT NUMBER: KITTING                                 **
      **                                                          **
      **  IMPLEMENTATION DATE:  07/31/2003                        **
      **                                                          **
      **  PROGRAM DEVELOPER: SATHYA                               **
      **                                                          **
      **  DESCRIPTION:  COMPONENT TO HANDLE PRICING REQUESTS      **
      **                FOR KITS                                  **
      **  NOTES:                                                  **
      **  IMPORTANT!                                              **
      **  ----------                                              **
      **  ANY CHANGE TO THIS SHOULD ALSO BE MADE TO A6O011UB      **
      **  Modification Log:                                       **
      **     DATE  PRGM DESCRIPTION                               **
      **  __/__/__ ___  _________________________________________ **
MD1019*----------------------------------------------------------------*
MD1019*  10/03/2019   MDWIVE    PRB0051772 - ACQUISITION COST ISSUE   **
MD1019*                                                               **                                                                        
NK0616*  06/17/2016   NKIMBO    CR 16-0054 NEGATIVE REBATE            **
NK0616*                                                               **
NK0615*----------------------------------------------------------------*
NK0615*  06/26/2015   NKIMBO    CR 15-0054 NDP EXPAND OMGPR           **
NK0615*                                                               **       
NK0415*----------------------------------------------------------------*
NK0415*  04/24/2015   NKIMBO    CR 14-0140 REMOVE REFERENCE TO        **
NK0415*                          OMGPR-F-BYPASS-PRC-LOCK              **
NK0813*----------------------------------------------------------------*
NK0813* 08/01/13  CR#13-0361 KIMBO   MODIFIED FOR DC                   *
NK0813*                      NATHAN   FORMULARY                        *
KS0912*----------------------------------------------------------------*
KS0912* 09/14/12  CR#11-0093 KEERTHI     MODIFIED TO CALCULATE LUM &   *
KS0912*                      SHEKAR S    EXTRA DELIVERY FEES           *
KS0912*----------------------------------------------------------------*
KS0912* 09/14/12  CR#11-0093 KEERTHI     MODIFIED TO CALCULATE LUM &   *
KS0912*                      SHEKAR S    EXTRA DELIVERY FEES           *
KS0912*----------------------------------------------------------------*
NK0110* 01/12/10             N.  KIMBO  SYNC UP CODE BETWEEN  
NK0110*                      RM# 7986010   A6O011UB & A6O011U
NK0408*----------------------------------------------------------------*
NK0408* 04/05/08  CR070139   N.  KIMBO   EXPAND OMGPR                  *
NK0408*----------------------------------------------------------------*
MFMIGR* 04/08/06  CR050057   JIM NOLAN   MODIFIED SET STATEMENT TO     *
MFMIGR*           MFMIGR                 RETRIEVE CURRENT DATE IN ORDER*
MFMIGR*                                  TO WORK WITH ANSI COMPLIANT   *
MFMIGR*                                  DBRM DATABASES.               *
MFMIGR*----------------------------------------------------------------*
      ** 03/04/05 JMONSO IM #2118549 - RLSE 5.3                   **
      ** WHEN PRICING A SUB PACK COMPUTE SUB PACK EXT ACQ COST TO **
      ** BE DISPLAYED BY AX609 NET OPTION. REMOVE SUBPACK TOTAL   **
      ** COST ADJUSTMENT FROM THE SUB PACKS UNIT COST. PASS       **
      ** OVERHEAD, THIRD PARTY FEES, & TOTAL COST ADJUSTMENTS     **
      ** FOR THE SUBPACKS TO AX609. NOTE THESE CHANGES IMPACT     **
      ** ONLINES ONLY AND WERE NOT APPLIED TO THE BATCH VERSION.  **
      **************************************************************
      ** WITH THE PACK TOTAL COST ADJUSTMENTS.                    **
      ** 04/10/04 SATHYA INCIDENT#1088002                         **
      ** IN CASE OF PRICING FOR ALT UOM FOR KIT, PASS TO PRICER   **
      ** ONLY BASE UOM                                            **
      **************************************************************
       ENVIRONMENT DIVISION.
       DATA DIVISION.
      *****************************************************************
      ***   W O R K I N G     S T O R A G E      S E C T I O N       **
      *****************************************************************
       WORKING-STORAGE SECTION.
       01  WS.
           05  WS-MARKER                    PIC X(14)
                                            VALUE 'A6U10 BEGINS>>'.
           05  WS-TRANSID                   PIC X(4)
                                            VALUE 'A610'.
      *
           05  WS-CURRENT-DATE             PIC X(10).
           05  WS-CONSTANTS.
               10  FILLER                   PIC X(12)
                                            VALUE 'CONSTANTS==>'.
               10  WS-CURRENT-PROGRAM       PIC X(08)
                                            VALUE 'A6O010U '.
               10  WS-CURRENT-TRANSACTION   PIC X(04)
                                            VALUE 'A610'.
               10  A6W013U-PROGRAM       PIC X(08)      VALUE 'A6O013U'.
               10  A6W012U-PROGRAM       PIC X(08)      VALUE 'A6O012U'.
               10  A6U01-PROGRAM       PIC X(08)      VALUE 'A6U01'.
               10  WS-AMOUNT-LIMIT     PIC 9(05)      VALUE 99999.
               10  WS-ERROR-TYPES.
                   15 WS-ABEND-ERROR        PIC X(01) VALUE 'A'.
                   15 WS-NONABEND-ERROR     PIC X(01) VALUE 'E'.
                   15 WS-WARNING            PIC X(01) VALUE 'W'.
               10  WS-NEXT-ERROR-NBR-X.
                   15  WS-NEXT-ERROR-NBR    PIC 9(03)
                                            VALUE 003.
SA0404         10  WS-UOM-KIT               PIC X(02) VALUE 'KT'.
           05  WS-WORK-AREAS.
               10  FILLER                   PIC X(12)
                                            VALUE 'WORK AREAS=>'.
               10 WS-LENGTH-TO-PASS         PIC S9(4) USAGE COMP SYNC
                                             VALUE ZERO.
               10 WS-EDITED-SQLCODE         PIC -Z999.
SA0404         10 WS-SAVED-UOM-ORDERED      PIC X(02) VALUE SPACES.
               10  WS-FROM-DATE-XX           PIC X(10).
               10  WS-TO-DATE-XX             PIC X(10).
               10  WS-FROM-DATE.
                   15  WS-FROM-DATE-YYYY     PIC 9(04).
                   15  WS-FROM-DATE-MM       PIC 9(02).
                   15  WS-FROM-DATE-DD       PIC 9(02).
               10  WS-TO-DATE.
                   15  WS-TO-DATE-YYYY       PIC 9(04).
                   15  WS-TO-DATE-MM         PIC 9(02).
                   15  WS-TO-DATE-DD         PIC 9(02).
               10  WS-FIND-GREAT-OR-LESS     PIC X(01).
                   88 WS-FIND-GREATER          VALUE 'G'.
                   88 WS-FIND-LESSER           VALUE 'L'.
               10  WS-DATE-ANSWER            PIC X(10).
               10 WS-ERR-MSG.
                  15 WS-ERR-NUM             PIC X(05).
                  15 WS-ERROR-MESSAGE       PIC X(70).
               10 WS-SQL-ERR-MSG.
                  15 WS-SQL-ERR-NUM             PIC X(05).
                  15 WS-SQL-ERROR-MESSAGE       PIC X(70).
NK0615         10 WS-OMGPR-COPY             PIC X(1789) VALUE SPACES.
               10 WS-COMPONENT-DATA.
                   15 WS-COMP-INFO-MARKER PIC X(10) VALUE 'COMP COST>'.
                   15 SUB                   PIC 9(3) VALUE 0.
                   15 WS-TEMP-COST           PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-TOTAL-ADJ-COST PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-OVERHEAD-CHG  PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-INFRT           PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-JIT-COST        PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-JIT-APPLY-FEE   PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-JIT-SERVICE-FEE PIC S9(06)V9(08) COMP-3.
KS0912             15 WS-PACK-JIT-LUM-FEE     PIC S9(06)V9(08) COMP-3.
KS0912             15 WS-PACK-JIT-EXTRA-DELIV-FEE
KS0912                                        PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-JIT-LABEL-FEE   PIC S9(06)V9(08) COMP-3.
                   15 WS-A-JIT-BREAK-FEE      PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-VNDCOSTADJ      PIC S9(06)V9(08) COMP-3.
                   15 WS-A-CUR-ADD-DISC-REB   PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-CNT-LN-MAX-REBT PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-CUR-TOTAL-REBATE PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-CUR-SELL-CST-DIF PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-CUR-VND-DIST-REB PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-CUR-CONT-REBATE PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-CNT-LN-PROT-ACQ PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-CNT-LN-UNIT-COST PIC S9(06)V9(08) COMP-3.
                   15 WS-PACK-TOTAL-COST   PIC S9(07)V9(8) USAGE COMP-3.
                   15 WS-PACK-CST-FOR-SELL PIC S9(07)V9(8) USAGE COMP-3.
                   15 WS-PACK-TOTAL-ACQCST PIC S9(07)V9(8) USAGE COMP-3.
JM0305             15 WS-SUBPACK-TOTAL-ACQCST PIC S9(07)V9(8) USAGE
                                                   COMP-3.
                   15 WS-PACK-TOTAL-CSTADJ PIC S9(07)V9(8) USAGE COMP-3.
                   15 WS-PACK-TOTAL-ADJCST PIC S9(07)V9(8) USAGE COMP-3.
                   15 WS-COMP-INFO OCCURS 700 TIMES.
                       20 WS-COMP-PROD-NO         PIC X(12).
                       20 WS-COMP-UOM             PIC X(02).
                       20 WS-COMP-QTY             PIC 9(05).
                       20 WS-COST                 PIC S9(5)V9(8) USAGE
                                                       COMP-3.
                       20 WS-ACQ-COST             PIC S9(5)V9(8) USAGE
                                                       COMP-3.
                   15 WS-COMP-INFO-MARKR-E        PIC X(01) VALUE '<'.
               10 WS-FULL-PRODUCT.
                  15 WS-VENDOR              PIC X(04).
                  15 WS-PRODUCT             PIC X(08).
               10 WS-XCTL-CICS-RESP-X.
                   15 WS-XCTL-CICS-RESP     PIC S9(8)
                                            VALUE     ZERO.

           05  WS-SWITCHES.
               10 WS-ERROR-SW              PIC X(01) VALUE SPACES.
                  88 WS-ERROR                VALUE 'Y'.
      *    COPY BOOK USED AS PARM FOR THIS SUBROUTINE
      *01  WS-WHAT-TO-PASS.
                   COPY OMGPR.
       02  A6U01-PARM-LIST  REDEFINES OMGPR-C.
NK0615     05 A6U01-PARM-REC                PIC X(1789).
       01  A6W013U-PARM-LIST.
                   COPY A6R13.
       01  A6W012U-PARM-LIST.
                   COPY OMGEXPL.
      *
           EXEC SQL
                INCLUDE DFHRESP
           END-EXEC.
           EXEC SQL
             INCLUDE  VNG02
           END-EXEC.
           EXEC SQL
             INCLUDE  VNG05
           END-EXEC.
*******    DB2 DEFINITIONS & OTHER INCLUDES
           EXEC SQL
               INCLUDE SQLCA
           END-EXEC.
      *****************************************************************
      ***              L I N K A G E          S E C T I O N          **
      *****************************************************************
       LINKAGE SECTION.
       01  DFHCOMMAREA.
NK0615     05  COMMAREA                     PIC X(1789).
       PROCEDURE DIVISION.
           PERFORM 0010-INIT
             THRU  0010-EXIT.
           PERFORM 0020-PROCESS
             THRU  0020-EXIT.
           PERFORM 0030-EOJ
             THRU  0030-EXIT.

       0010-INIT.
NK0615     MOVE 1789                           TO WS-LENGTH-TO-PASS.
           MOVE DFHCOMMAREA                    TO OMGPR.
           INITIALIZE OMGPR-PRC-OUTPUT-FIELDS.
           INITIALIZE OMGEXPL-PRC-OUTPUT-FIELDS
           INITIALIZE A6W13-RTND-DATA.
           EXEC SQL
MFMIGR        SELECT CONVERT(CHAR(10),CURRENT_TIMESTAMP,101)
MFMIGR               INTO :WS-CURRENT-DATE
MFMIGR
           END-EXEC.
           EVALUATE SQLCODE
              WHEN 0
                 CONTINUE
              WHEN OTHER
                 MOVE 'Y'                TO OMGPR-F-PRICER-ERROR
                 MOVE '61110- SQL ERROR  '
                                         TO WS-SQL-ERROR-MESSAGE
                 MOVE SQLCODE             TO WS-SQL-ERR-NUM
                 MOVE WS-SQL-ERR-MSG      TO OMGPR-ERROR-MESSAGE
           END-EVALUATE.

       0010-EXIT.
           EXIT.

       0020-PROCESS.
           IF OMGPR-PRICER-ERROR
              GO TO 0020-EXIT
           END-IF.
           PERFORM 1000-VALIDATE-INPUT
             THRU  1000-EXIT.
           PERFORM 0100-GET-CATEGORY
             THRU  0100-EXIT.
           IF A6W13-ABEND OR
              A6W13-ERROR
              PERFORM 9998-BUILD-ERROR-MSG
                THRU  9998-EXIT
              MOVE WS-ERR-MSG            TO OMGPR-ERROR-MESSAGE
              MOVE 'Y'                   TO OMGPR-F-PRICER-ERROR
              GO TO 0020-EXIT
           END-IF.
           PERFORM 0200-EXPLODE
             THRU  0200-EXIT.
           IF OMGEXPL-ABEND OR
              OMGEXPL-ERROR
              PERFORM 9999-BUILD-ERROR-MSG
                THRU  9999-EXIT
              MOVE WS-ERR-MSG            TO OMGPR-ERROR-MESSAGE
              MOVE 'Y'                   TO OMGPR-F-PRICER-ERROR
              GO TO 0020-EXIT
           END-IF.
           PERFORM 2000-PROCESS-PRICE
             THRU  2000-EXIT.
           IF WS-ERROR
             IF WS-ERR-MSG NOT = SPACES
               MOVE WS-ERR-MSG           TO OMGPR-ERROR-MESSAGE
             END-IF
           END-IF.
       0020-EXIT.
           EXIT.
      *************************
       0030-EOJ.
      *************************
           MOVE 'O'                            TO OMGPR-C-PRODUCT-TYPE
           MOVE OMGPR                          TO DFHCOMMAREA.
           PERFORM 8000-PROCESS-RETURN
             THRU  8000-EXIT.


       0030-EXIT.
           EXIT.

       0100-GET-CATEGORY.
           MOVE OMGPR-I-VENDOR             TO A6W13-I-VENDOR.
           MOVE OMGPR-I-VND-PRODUCT        TO A6W13-I-VND-PRODUCT.
           MOVE OMGPR-C-ORD-LIN-CUST-UOM   TO A6W13-UOM.
           MOVE OMGPR-I-DIVISION           TO A6W13-BRANCH.
           MOVE SPACES                     TO A6W13-UNUSED-1.
           PERFORM 9020-LINK-TO-A6O013
             THRU  9020-EXIT.
           EVALUATE TRUE
               WHEN WS-XCTL-CICS-RESP =
                    DFHRESP-NORMAL
                    MOVE A6W13-S-PROD-CATEGORY
                                         TO OMGPR-S-PROD-CATEGORY
                    MOVE A6W13-BASE-UOM  TO OMGPR-C-VND-PROD-BASE-UM
                    MOVE A6W13-INV-CLASS TO OMGPR-C-INV-CLASS
                    IF OMGPR-C-ORD-LIN-CUST-UOM NOT =
                               OMGPR-C-VND-PROD-BASE-UM
                       MOVE OMGPR-C-ORD-LIN-CUST-UOM
                                         TO OMGPR-ALT-ORD-UM
                       MOVE A6W13-CONV-FACTOR
                                         TO OMGPR-ALT-ORD-CONV-FACTOR
                       MOVE A6W13-PROD-CAT-EXP-DATE
                                         TO OMGPR-D-EXPIRATION
                    END-IF
               WHEN WS-XCTL-CICS-RESP =
                    DFHRESP-PGMIDERR
                 MOVE 'A6O013U TRAN IS UNAVAILABLE'
                                         TO OMGPR-ERROR-MESSAGE
                    MOVE 'Y'             TO OMGPR-F-PRICER-ERROR
               WHEN OTHER
                   MOVE 'CICS LINK ERROR TO A6O013U'
                                         TO OMGPR-ERROR-MESSAGE
                   MOVE 'Y'             TO OMGPR-F-PRICER-ERROR
           END-EVALUATE.

       0100-EXIT.
           EXIT.

       0200-EXPLODE.
           MOVE OMGPR-I-VENDOR             TO WS-VENDOR.
           MOVE OMGPR-I-VND-PRODUCT        TO WS-PRODUCT.
           MOVE WS-FULL-PRODUCT            TO OMGEXPL-PACK-PRODNO.
           MOVE OMGPR-D-PRICING            TO OMGEXPL-INP-EFF-DATE.
           MOVE 'C'                        TO OMGEXPL-REQUEST-TYPE.
           MOVE SPACES                     TO OMGEXPL-T-NEXT-PRODUCT-NO
                                              OMGEXPL-ROLLUP-COST-SW
                                              OMGEXPL-T-INPUT-FILLER.
           PERFORM 9015-LINK-TO-EXPLODER
             THRU  9015-EXIT.
           EVALUATE TRUE
               WHEN WS-XCTL-CICS-RESP =
                    DFHRESP-NORMAL
                   CONTINUE
               WHEN WS-XCTL-CICS-RESP =
                    DFHRESP-PGMIDERR
                 MOVE 'A6O012U TRAN IS UNAVAILABLE'
                                         TO OMGPR-ERROR-MESSAGE
                    MOVE 'Y'             TO OMGPR-F-PRICER-ERROR
               WHEN OTHER
                   MOVE 'CICS LINK ERROR TO A6O012U'
                                         TO OMGPR-ERROR-MESSAGE
                   MOVE 'Y'             TO OMGPR-F-PRICER-ERROR
           END-EVALUATE.

       0200-EXIT.
           EXIT.

       1000-VALIDATE-INPUT.
            CONTINUE.

       1000-EXIT.
           EXIT.
      *
       2000-PROCESS-PRICE.
           MOVE OMGPR                    TO WS-OMGPR-COPY.
           MOVE SPACES                   TO WS-ERROR-SW.
           MOVE 0                        TO WS-PACK-TOTAL-COST.
           MOVE 0                        TO WS-PACK-TOTAL-ADJCST
                                            WS-PACK-TOTAL-ADJ-COST
                                            WS-PACK-TOTAL-ACQCST
JM0305                                      WS-SUBPACK-TOTAL-ACQCST
                                            WS-PACK-TOTAL-CSTADJ
                                            WS-PACK-OVERHEAD-CHG
                                            WS-PACK-INFRT
                                            WS-PACK-JIT-COST
                                            WS-PACK-JIT-APPLY-FEE
                                            WS-PACK-JIT-SERVICE-FEE
KS0912                                      WS-PACK-JIT-LUM-FEE
KS0912                                      WS-PACK-JIT-EXTRA-DELIV-FEE
                                            WS-PACK-JIT-LABEL-FEE
                                            WS-A-JIT-BREAK-FEE
                                            WS-PACK-VNDCOSTADJ
                                            WS-A-CUR-ADD-DISC-REB
                                            WS-PACK-CNT-LN-MAX-REBT
                                            WS-PACK-CUR-TOTAL-REBATE
                                            WS-PACK-CUR-SELL-CST-DIF
                                            WS-PACK-CUR-VND-DIST-REB
                                            WS-PACK-CUR-CONT-REBATE
                                            WS-PACK-CNT-LN-PROT-ACQ
                                            WS-PACK-CNT-LN-UNIT-COST.
           PERFORM VARYING SUB FROM 1 BY 1
                UNTIL SUB >  OMGEXPL-NUMBER-OF-ITEMS
                   OR WS-ERROR
                   OR OMGPR-PRICER-ERROR
                MOVE OMGEXPL-COMP-PROD-NO(SUB) TO WS-FULL-PRODUCT
                MOVE WS-VENDOR            TO OMGPR-I-VENDOR
                MOVE WS-PRODUCT           TO OMGPR-I-VND-PRODUCT
                MOVE OMGEXPL-COMP-UOM(SUB) TO OMGPR-C-ORD-LIN-CUST-UOM
                PERFORM 7000-INIT-FOR-COMP-COST
                  THRU  7000-EXIT
                PERFORM 7100-HANDLE-ALTUOM-COMP
                  THRU  7100-EXIT
                PERFORM 9000-LINK-TO-PRICER
                  THRU  9000-EXIT
                IF OMGPR-PRICER-ERROR
                   MOVE OMGPR-Q-ERROR-NBR  TO WS-ERR-NUM
                   MOVE OMGPR-ERROR-MESSAGE TO WS-ERROR-MESSAGE
                   MOVE 'Y'              TO WS-ERROR-SW
                   GO TO 2000-EXIT
                END-IF
                PERFORM 3000-COMPUTE-COST
                  THRU  3000-EXIT
MD1019          IF OMGPR-PRICER-ERROR
MD1019             MOVE OMGPR-Q-ERROR-NBR  TO WS-ERR-NUM
MD1019             MOVE OMGPR-ERROR-MESSAGE TO WS-ERROR-MESSAGE
MD1019             MOVE 'Y'              TO WS-ERROR-SW
MD1019             GO TO 2000-EXIT
MD1019          END-IF                  
           END-PERFORM.
           PERFORM 3100-MOVE-COSTS
             THRU  3100-EXIT.
           PERFORM 4000-COMPUTE-SELL
             THRU  4000-EXIT.
           IF OMGPR-PRICER-ERROR
                   MOVE OMGPR-Q-ERROR-NBR  TO WS-ERR-NUM
                   MOVE OMGPR-ERROR-MESSAGE TO WS-ERROR-MESSAGE
                   MOVE 'Y'              TO WS-ERROR-SW
                   GO TO 2000-EXIT
           END-IF.
           PERFORM 4200-PROCESS-EXP-DATE
             THRU  4200-EXIT.
           IF OMGPR-PRICER-ERROR
                   MOVE OMGPR-Q-ERROR-NBR  TO WS-ERR-NUM
                   MOVE OMGPR-ERROR-MESSAGE TO WS-ERROR-MESSAGE
                   MOVE 'Y'              TO WS-ERROR-SW
                   GO TO 2000-EXIT
           END-IF.
SA0803     IF OMGPR-C-ORD-LIN-CUST-UOM NOT =
SA0803                         OMGPR-C-VND-PROD-BASE-UM
SA0803        PERFORM 4300-HANDLE-ALT-UOM-KIT
SA0803          THRU  4300-EXIT
SA0803     END-IF.
       2000-EXIT.
           EXIT.
      *
       3000-COMPUTE-COST.
            MOVE OMGPR-I-VENDOR          TO WS-VENDOR.
            MOVE OMGPR-I-VND-PRODUCT     TO WS-PRODUCT.
            MOVE WS-FULL-PRODUCT         TO WS-COMP-PROD-NO(SUB).
            MOVE OMGPR-C-ORD-LIN-CUST-UOM
                                         TO WS-COMP-UOM (SUB).
            MOVE OMGEXPL-COMP-QTY(SUB)   TO WS-COMP-QTY(SUB).
            COMPUTE WS-COST(SUB) =
NK0110                   WS-COMP-QTY(SUB) * OMGPR-A-TOTAL-COST.
            ADD WS-COST(SUB)             TO WS-PACK-TOTAL-COST.
            COMPUTE WS-ACQ-COST(SUB) = WS-COMP-QTY(SUB)
                                          * OMGPR-A-VND-PRC-DEALER.
            IF OMGPR-A-VND-PRC-DEALER > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-VND-PRC-DEALER
              ADD WS-TEMP-COST           TO WS-PACK-TOTAL-ACQCST
            END-IF.
JM0405*** COMPUTE TOTAL ACQUISITION COST FOR A SUBPACK.
JM0405*** FOR RELEASE 5.3. IM #2118549.
JM0405      IF OMGPR-A-VND-PRC-ACQ-COST > 0
JM0405        COMPUTE WS-TEMP-COST =
JM0405                   WS-COMP-QTY(SUB) * OMGPR-A-VND-PRC-ACQ-COST
JM0405        ADD WS-TEMP-COST           TO WS-SUBPACK-TOTAL-ACQCST
JM0405      END-IF.
            IF OMGPR-A-TOTAL-ADJ-COST > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-TOTAL-ADJ-COST
              ADD WS-TEMP-COST           TO WS-PACK-TOTAL-ADJCST
              ADD WS-TEMP-COST           TO WS-PACK-TOTAL-ADJ-COST
              ADD WS-TEMP-COST           TO WS-PACK-TOTAL-CSTADJ
            END-IF.
            IF OMGPR-A-OVERHEAD-CHG > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-OVERHEAD-CHG
              ADD WS-TEMP-COST           TO WS-PACK-OVERHEAD-CHG
            END-IF.
            IF OMGPR-A-INFRT > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-INFRT
              ADD WS-TEMP-COST           TO WS-PACK-INFRT
            END-IF.
            IF OMGPR-A-JIT-COST > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-JIT-COST
              ADD WS-TEMP-COST           TO WS-PACK-JIT-COST
            END-IF.
            IF OMGPR-A-JIT-APPLY-FEE > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-JIT-APPLY-FEE
              ADD WS-TEMP-COST           TO WS-PACK-JIT-APPLY-FEE
            END-IF.
            IF OMGPR-A-JIT-SERVICE-FEE > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-JIT-SERVICE-FEE
              ADD WS-TEMP-COST           TO WS-PACK-JIT-SERVICE-FEE
            END-IF.
KS0912      IF OMGPR-A-JIT-LUM-FEE > 0
KS0912        COMPUTE WS-TEMP-COST =
KS0912                   WS-COMP-QTY(SUB) * OMGPR-A-JIT-LUM-FEE
KS0912        ADD WS-TEMP-COST           TO WS-PACK-JIT-LUM-FEE
KS0912      END-IF.
KS0912      IF OMGPR-A-JIT-EXTRA-DELIV-FEE > 0
KS0912        COMPUTE WS-TEMP-COST =
KS0912                   WS-COMP-QTY(SUB) * OMGPR-A-JIT-EXTRA-DELIV-FEE
KS0912        ADD WS-TEMP-COST           TO WS-PACK-JIT-EXTRA-DELIV-FEE
KS0912      END-IF.
            IF OMGPR-A-JIT-LABEL-FEE > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-JIT-LABEL-FEE
              ADD WS-TEMP-COST           TO WS-PACK-JIT-LABEL-FEE
            END-IF.
            IF OMGPR-A-JIT-BREAK-FEE > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-JIT-BREAK-FEE
              ADD WS-TEMP-COST           TO WS-A-JIT-BREAK-FEE
            END-IF.
            IF OMGPR-A-VNDCOSTADJ > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-VNDCOSTADJ
              ADD WS-TEMP-COST           TO WS-PACK-VNDCOSTADJ
            END-IF.
            IF OMGPR-A-CUR-ADD-DISC-REB > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-CUR-ADD-DISC-REB
              ADD WS-TEMP-COST           TO WS-A-CUR-ADD-DISC-REB
            END-IF.
            IF OMGPR-A-CNT-LN-MAX-REBT > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-CNT-LN-MAX-REBT
              ADD WS-TEMP-COST           TO WS-PACK-CNT-LN-MAX-REBT
            END-IF.
NK0616      IF OMGPR-A-CUR-TOTAL-REBATE  <> 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-CUR-TOTAL-REBATE
              ADD WS-TEMP-COST           TO WS-PACK-CUR-TOTAL-REBATE
            END-IF.
            IF OMGPR-A-CUR-SELL-CST-DIF > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-CUR-SELL-CST-DIF
              ADD WS-TEMP-COST           TO WS-PACK-CUR-SELL-CST-DIF
            END-IF.
            IF OMGPR-A-CUR-VND-DIST-REB > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-CUR-VND-DIST-REB
              ADD WS-TEMP-COST           TO WS-PACK-CUR-VND-DIST-REB
            END-IF.
NK0616      IF OMGPR-A-CUR-CONT-REBATE <> 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-CUR-CONT-REBATE
              ADD WS-TEMP-COST           TO WS-PACK-CUR-CONT-REBATE
            END-IF.
            IF OMGPR-A-CNT-LN-PROT-ACQ > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-CNT-LN-PROT-ACQ
              ADD WS-TEMP-COST           TO WS-PACK-CNT-LN-PROT-ACQ
            END-IF.
            IF OMGPR-A-CNT-LN-UNIT-COST > 0
              COMPUTE WS-TEMP-COST =
                         WS-COMP-QTY(SUB) * OMGPR-A-CNT-LN-UNIT-COST
              ADD WS-TEMP-COST           TO WS-PACK-CNT-LN-UNIT-COST
            END-IF.
            IF (WS-PACK-CNT-LN-UNIT-COST > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-TOTAL-ADJCST    > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-TOTAL-ADJ-COST  > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-TOTAL-ACQCST    > WS-AMOUNT-LIMIT ) OR
JM0305        ( WS-SUBPACK-TOTAL-ACQCST > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-OVERHEAD-CHG    > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-INFRT           > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-JIT-COST        > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-JIT-APPLY-FEE   > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-JIT-SERVICE-FEE > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-JIT-LABEL-FEE   > WS-AMOUNT-LIMIT ) OR
              ( WS-A-JIT-BREAK-FEE      > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-VNDCOSTADJ      > WS-AMOUNT-LIMIT ) OR
              ( WS-A-CUR-ADD-DISC-REB   > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-CNT-LN-MAX-REBT > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-CUR-TOTAL-REBATE > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-CUR-SELL-CST-DIF > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-CUR-VND-DIST-REB > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-CUR-CONT-REBATE  > WS-AMOUNT-LIMIT ) OR
              ( WS-PACK-CNT-LN-PROT-ACQ  > WS-AMOUNT-LIMIT )

                   MOVE 'PACK SELL/COST AMT EXCEEDS LIMIT'
                                         TO OMGPR-ERROR-MESSAGE
MD1019             MOVE  3000            TO OMGPR-Q-ERROR-NBR           
                   MOVE 'Y'             TO OMGPR-F-PRICER-ERROR
           END-IF.
       3000-EXIT.
           EXIT.
      *
       3100-MOVE-COSTS.
      *
           MOVE WS-OMGPR-COPY            TO OMGPR.
      *
           COMPUTE WS-PACK-TOTAL-ACQCST = WS-PACK-TOTAL-ACQCST
                                  + OMGEXPL-OH-FEE
                                  + OMGEXPL-TP-FEE-COST
                                  + OMGEXPL-TP-FEE-SELL.
           MOVE WS-PACK-TOTAL-ACQCST TO OMGPR-A-VND-PRC-DEALER.
      *
JM0405     MOVE WS-SUBPACK-TOTAL-ACQCST TO OMGPR-A-VND-PRC-ACQ-COST.
      *
           COMPUTE WS-PACK-TOTAL-COST = WS-PACK-TOTAL-COST
                                  + OMGEXPL-OH-FEE
                                  + OMGEXPL-TP-FEE-COST
                                  + OMGEXPL-TP-FEE-SELL.
           MOVE WS-PACK-TOTAL-COST       TO OMGPR-A-TOTAL-COST.
      *
           COMPUTE WS-PACK-CNT-LN-UNIT-COST =
                                    WS-PACK-CNT-LN-UNIT-COST
                                  + OMGEXPL-OH-FEE
                                  + OMGEXPL-TP-FEE-COST
                                  + OMGEXPL-TP-FEE-SELL.
JM0405*** REMOVED SUBPACK TOTAL COST ADJUSTMENT FROM PACK UNIT COST
JM0405*** FOR RELEASE 5.3. IM #2118549.
JM0405*                           + WS-PACK-TOTAL-CSTADJ.
           MOVE WS-PACK-CNT-LN-UNIT-COST TO OMGPR-A-CNT-LN-UNIT-COST.
      *
           COMPUTE WS-PACK-CST-FOR-SELL = WS-PACK-TOTAL-COST.
SA0803     IF OMGEXPL-TP-FEE-SELL > 0
SA0803        COMPUTE WS-PACK-CST-FOR-SELL = WS-PACK-TOTAL-COST -
SA0803                                          OMGEXPL-TP-FEE-SELL
SA0803     END-IF.
      *
JM0405*** REMOVED OVERHEAD & THIRD PARTY FEES FROM SUBPACK TOTAL COST
JM0405*** ADJUSTMENT FOR RELEASE 5.3. IM #2118549.
JM0405*    COMPUTE WS-PACK-TOTAL-ADJ-COST = WS-PACK-TOTAL-CSTADJ.
JM0405*                           + OMGEXPL-OH-FEE
JM0405*                           + OMGEXPL-TP-FEE-COST.
      *    WE DO NOT WANT TO SHOW COST ADJ IN CASE OF KITS
JM0405*** ADDED SUBPACK TOTAL COST ADJUSTMENT TO TOTAL COST ADJUSTMENT
JM0405*** FOR A PACK. PART OF RELEASE 5.3. IM #2118549. USED BY AX609.
JM0405     MOVE WS-PACK-TOTAL-ADJ-COST   TO OMGPR-A-TOTAL-ADJ-COST.
JM0405*** COMPUTE OVERHEAD & THIRD PARTY FEES FOR A PACK. USED BY AX609.
JM0405*** PART OF RELEASE 5.3. IM #2118549.
JM0405     COMPUTE WS-PACK-OVERHEAD-CHG = WS-PACK-OVERHEAD-CHG
JM0405                                  + OMGEXPL-OH-FEE.
           MOVE WS-PACK-OVERHEAD-CHG     TO OMGPR-A-OVERHEAD-CHG
JM0405     COMPUTE OMGPR-A-THIRDPARTY-FEE = OMGEXPL-TP-FEE-COST
JM0405                                    + OMGEXPL-TP-FEE-SELL.
JM0405*** USING THIS SWITCH TO MIMICK THE AP_3RD_PRTY_FEE_SW FROM VNG30
JM0405*** SO THE PROPER LITERAL CAN BE DISPLAYED BY AX609.
JM0405     IF OMGEXPL-TP-FEE-SELL > 0
JM0405       MOVE 'A'                TO OMGPR-C-APLY-TRDPTY-BEFMKUP-SW
JM0405     ELSE
JM0405       MOVE 'B'                TO OMGPR-C-APLY-TRDPTY-BEFMKUP-SW.
           MOVE WS-PACK-INFRT            TO OMGPR-A-INFRT
           MOVE WS-PACK-JIT-COST         TO OMGPR-A-JIT-COST
           MOVE WS-PACK-JIT-APPLY-FEE    TO OMGPR-A-JIT-APPLY-FEE
           MOVE WS-PACK-JIT-SERVICE-FEE  TO OMGPR-A-JIT-SERVICE-FEE
KS0912     MOVE WS-PACK-JIT-LUM-FEE      TO OMGPR-A-JIT-LUM-FEE
KS0912     MOVE WS-PACK-JIT-EXTRA-DELIV-FEE
KS0912                                   TO OMGPR-A-JIT-EXTRA-DELIV-FEE
           MOVE WS-PACK-JIT-LABEL-FEE    TO OMGPR-A-JIT-LABEL-FEE
           MOVE WS-A-JIT-BREAK-FEE       TO OMGPR-A-JIT-BREAK-FEE
           MOVE WS-PACK-VNDCOSTADJ       TO OMGPR-A-VNDCOSTADJ
           MOVE WS-A-CUR-ADD-DISC-REB    TO OMGPR-A-CUR-ADD-DISC-REB
           MOVE WS-PACK-CNT-LN-MAX-REBT  TO OMGPR-A-CNT-LN-MAX-REBT
           MOVE WS-PACK-CUR-TOTAL-REBATE
                                     TO OMGPR-A-CUR-TOTAL-REBATE
           MOVE WS-PACK-CUR-SELL-CST-DIF TO
                                        OMGPR-A-CUR-SELL-CST-DIF
           MOVE WS-PACK-CUR-VND-DIST-REB TO OMGPR-A-CUR-VND-DIST-REB
           MOVE WS-PACK-CUR-CONT-REBATE TO OMGPR-A-CUR-CONT-REBATE
           MOVE WS-PACK-CNT-LN-PROT-ACQ TO OMGPR-A-CNT-LN-PROT-ACQ
SA0803** FOR KITS DO NOT OUTPUT ANY COST ADJ IN AX606.
JM0405** OVERHEAD CHARGE & TOTAL ADJ COST WILL BE SUPPRESSED IN AX606.
SA0803     MOVE 0                        TO OMGPR-A-CUST-INV-CLASS-CT
SA0803                                      OMGPR-P-CUST-INV-CLASS-CT
JM0405**                                    OMGPR-A-OVERHEAD-CHG
JM0405**                                    OMGPR-A-TOTAL-ADJ-COST
SA0803                                      OMGPR-P-OVERHEAD-CHG
SA0803                                      OMGPR-A-INFRT
SA0803                                      OMGPR-P-INFRT-BG
SA0803                                      OMGPR-A-JIT-COST
SA0803                                      OMGPR-A-VNDCOSTADJ
SA0803                                      OMGPR-P-VNDCOSTADJ
           .
       3100-EXIT.
           EXIT.
      *
       4000-COMPUTE-SELL.
           MOVE 'N'                      TO OMGPR-F-JIT-EXEMPT.
           MOVE 'N'                      TO OMGPR-F-FRT-EXEMPT.
           IF OMGPR-F-JIT-EXEMPT = 'Y'
              CONTINUE
           ELSE
      *     JIT ADJ ON COST
              IF ( OMGPR-JIT-SERVICE-FEE = 'A') OR
                 (OMGPR-JIT-SERVICE-FEE = 'C')
                  IF OMGPR-F-ST-JIT-CUSTOMER = 'Y'
                     MOVE WS-PACK-CST-FOR-SELL TO OMGPR-A-TOTAL-COST
                     PERFORM 4010-GET-JIT-ON-COST
                       THRU  4010-EXIT
                     MOVE WS-PACK-TOTAL-COST   TO OMGPR-A-TOTAL-COST
                  END-IF
              END-IF
           END-IF.
           PERFORM 4020-GET-SELL
             THRU  4020-EXIT.
           IF OMGEXPL-TP-FEE-SELL > 0
      * NO NEED TO SHOW FEE ON SELL FOR KITS AS SELL ADJUSTMENTS
***********   ADD OMGEXPL-TP-FEE-SELL TO OMGPR-A-TOTAL-ADJ-SELL
              ADD OMGEXPL-TP-FEE-SELL TO OMGPR-A-TOTAL-SELL
              ADD OMGEXPL-TP-FEE-SELL TO OMGPR-A-CUS-UOM-SELL-PRC
              ADD OMGEXPL-TP-FEE-SELL TO OMGPR-A-TOTAL-SELL-UN
              IF ( OMGPR-A-TOTAL-SELL  > WS-AMOUNT-LIMIT )

                   MOVE 'SELL AMT  EXCEEDS LIMIT'
                                         TO OMGPR-ERROR-MESSAGE
                   MOVE 'Y'             TO OMGPR-F-PRICER-ERROR
                   MOVE  4000            TO OMGPR-Q-ERROR-NBR
              END-IF
              IF ( OMGPR-A-TOTAL-ADJ-SELL  > WS-AMOUNT-LIMIT )

                   MOVE 'SELL ADJ AMT EXCEEDS LIMIT'
                                         TO OMGPR-ERROR-MESSAGE
                   MOVE 'Y'             TO OMGPR-F-PRICER-ERROR
                   MOVE  4000            TO OMGPR-Q-ERROR-NBR
              END-IF
           END-IF.
       4000-EXIT.
           EXIT.
      *
       4010-GET-JIT-ON-COST.
           MOVE 'J'                      TO OMGPR-PRICING-REQ-SW.
           PERFORM 9000-LINK-TO-PRICER
             THRU  9000-EXIT.
       4010-EXIT.
           EXIT.
      *
       4020-GET-SELL.
           MOVE 'S'                  TO OMGPR-PRICING-REQ-SW.
SA0404     MOVE OMGPR-C-ORD-LIN-CUST-UOM
SA0404                               TO WS-SAVED-UOM-ORDERED.
           MOVE WS-PACK-CST-FOR-SELL TO OMGPR-A-TOTAL-COST.
SA0404     MOVE WS-UOM-KIT           TO OMGPR-C-ORD-LIN-CUST-UOM.
           PERFORM 9000-LINK-TO-PRICER
             THRU  9000-EXIT.
           MOVE WS-PACK-TOTAL-COST   TO OMGPR-A-TOTAL-COST.
SA0404     MOVE WS-SAVED-UOM-ORDERED TO OMGPR-C-ORD-LIN-CUST-UOM.
       4020-EXIT.
           EXIT.
      *
       4200-PROCESS-EXP-DATE.
           IF OMGEXPL-OH-EXP-DATE > SPACES
             IF OMGPR-D-EXPIRATION > SPACES
                MOVE 'L' TO WS-FIND-GREAT-OR-LESS
                MOVE OMGPR-D-EXPIRATION     TO WS-FROM-DATE-XX
                MOVE OMGEXPL-OH-EXP-DATE    TO WS-TO-DATE-XX
                PERFORM 8800-COMPARE-DATES
                  THRU  8800-EXIT
                MOVE WS-DATE-ANSWER    TO OMGPR-D-EXPIRATION
            ELSE
              MOVE OMGEXPL-OH-EXP-DATE TO OMGPR-D-EXPIRATION
            END-IF
           END-IF.
           IF OMGEXPL-TP-COST-EXP-DATE > SPACES
             IF OMGPR-D-EXPIRATION > SPACES
                MOVE 'L' TO WS-FIND-GREAT-OR-LESS
                MOVE OMGPR-D-EXPIRATION     TO WS-FROM-DATE-XX
                MOVE OMGEXPL-TP-COST-EXP-DATE TO WS-TO-DATE-XX
                PERFORM 8800-COMPARE-DATES
                  THRU  8800-EXIT
                MOVE WS-DATE-ANSWER    TO OMGPR-D-EXPIRATION
            ELSE
              MOVE OMGEXPL-TP-COST-EXP-DATE TO OMGPR-D-EXPIRATION
            END-IF
           END-IF.
           IF OMGEXPL-TP-SELL-EXP-DATE > SPACES
             IF OMGPR-D-EXPIRATION > SPACES
                MOVE 'L' TO WS-FIND-GREAT-OR-LESS
                MOVE OMGPR-D-EXPIRATION     TO WS-FROM-DATE-XX
                MOVE OMGEXPL-TP-SELL-EXP-DATE TO WS-TO-DATE-XX
                PERFORM 8800-COMPARE-DATES
                  THRU  8800-EXIT
                MOVE WS-DATE-ANSWER    TO OMGPR-D-EXPIRATION
            ELSE
              MOVE OMGEXPL-TP-SELL-EXP-DATE TO OMGPR-D-EXPIRATION
            END-IF
           END-IF.
       4200-EXIT.
           EXIT.
      *
SA0803 4300-HANDLE-ALT-UOM-KIT.
SA0803     COMPUTE  OMGPR-A-VND-PRC-DEALER = OMGPR-ALT-ORD-CONV-FACTOR
SA0803                * OMGPR-A-VND-PRC-DEALER
SA0803*
SA0803     COMPUTE OMGPR-A-TOTAL-COST = OMGPR-A-TOTAL-COST
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803*
SA0803     COMPUTE OMGPR-A-CNT-LN-UNIT-COST = OMGPR-A-CNT-LN-UNIT-COST
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
      *
      *    WE DO NOT WANT TO SHOW COST ADJ IN CASE OF KITS
SA0803     COMPUTE OMGPR-A-OVERHEAD-CHG = OMGPR-A-OVERHEAD-CHG
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE  OMGPR-A-INFRT = OMGPR-A-INFRT
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-JIT-COST = OMGPR-A-JIT-COST
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-JIT-APPLY-FEE = OMGPR-A-JIT-APPLY-FEE
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-JIT-SERVICE-FEE = OMGPR-A-JIT-SERVICE-FEE
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
KS0912     COMPUTE OMGPR-A-JIT-LUM-FEE = OMGPR-A-JIT-LUM-FEE
KS0912                       * OMGPR-ALT-ORD-CONV-FACTOR
KS0912     COMPUTE OMGPR-A-JIT-EXTRA-DELIV-FEE =
KS0912                         OMGPR-A-JIT-EXTRA-DELIV-FEE
KS0912                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-JIT-LABEL-FEE = OMGPR-A-JIT-LABEL-FEE
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-JIT-BREAK-FEE = OMGPR-A-JIT-BREAK-FEE
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-VNDCOSTADJ = OMGPR-A-VNDCOSTADJ
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-CUR-ADD-DISC-REB = OMGPR-A-CUR-ADD-DISC-REB
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-CNT-LN-MAX-REBT = OMGPR-A-CNT-LN-MAX-REBT
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-CUR-TOTAL-REBATE = OMGPR-A-CUR-TOTAL-REBATE
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-CUR-SELL-CST-DIF= OMGPR-A-CUR-SELL-CST-DIF
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-CUR-VND-DIST-REB = OMGPR-A-CUR-VND-DIST-REB
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-CUR-CONT-REBATE = OMGPR-A-CUR-CONT-REBATE
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-CNT-LN-PROT-ACQ = OMGPR-A-CNT-LN-PROT-ACQ
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-TOTAL-SELL = OMGPR-A-TOTAL-SELL
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-TOTAL-SELL-UN = OMGPR-A-TOTAL-SELL-UN
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     COMPUTE OMGPR-A-CUS-UOM-SELL-PRC = OMGPR-A-CUS-UOM-SELL-PRC
SA0803                       * OMGPR-ALT-ORD-CONV-FACTOR
SA0803     .
SA0803 4300-EXIT.
SA0803     EXIT.
       7000-INIT-FOR-COMP-COST.
           MOVE SPACES                   TO OMGPR-JIT-DATA
NK0813                                      OMGPR-DATA-FOR-PACKS.
NK0813*                                     OMGPR-T-INPUT-FILLER.
           MOVE 'R'                      TO OMGPR-INPUT-PRODUCT-TYPE.
           MOVE 'C'                      TO OMGPR-PRICING-REQ-SW.
           INITIALIZE                       OMGPR-PRC-OUTPUT-FIELDS.
       7000-EXIT.
           EXIT.
      *
       7100-HANDLE-ALTUOM-COMP.
           MOVE OMGPR-I-VENDOR      TO VNG02-I-VENDOR
                                       VNG05-I-VENDOR.
           MOVE OMGPR-I-VND-PRODUCT TO VNG02-I-VND-PRODUCT
                                       VNG05-I-VND-PRODUCT.
           MOVE OMGPR-C-ORD-LIN-CUST-UOM TO VNG05-C-VD-PRD-ALT-UM.
           PERFORM 9490-SQL-SELECT-VNG02
              THRU 9490-EXIT
           IF OMGPR-PRICER-ERROR
              GO TO 7100-EXIT
           END-IF
           IF VNG02-C-VND-PROD-BASE-UM = OMGPR-C-ORD-LIN-CUST-UOM
              MOVE 1 TO OMGPR-ALT-ORD-CONV-FACTOR
              GO TO 7100-EXIT
           ELSE
               PERFORM 9495-SELECT-VNG05
                 THRU  9495-EXIT
           END-IF
           IF OMGPR-PRICER-ERROR
              GO TO 7100-EXIT
           END-IF
           MOVE VNG05-A-VD-PRD-ALT-UMF TO OMGPR-ALT-ORD-CONV-FACTOR.
       7100-EXIT.
           EXIT.
      *
       8000-PROCESS-RETURN.
           EXEC CICS RETURN END-EXEC.
           GOBACK.
       8000-EXIT.
           EXIT.
      **
      **
       8800-COMPARE-DATES.

            MOVE WS-FROM-DATE-XX(1:2)    TO WS-FROM-DATE-MM.
            MOVE WS-FROM-DATE-XX(4:2)    TO WS-FROM-DATE-DD.
            MOVE WS-FROM-DATE-XX(7:4)    TO WS-FROM-DATE-YYYY.
            MOVE WS-TO-DATE-XX(1:2)      TO WS-TO-DATE-MM.
            MOVE WS-TO-DATE-XX(4:2)      TO WS-TO-DATE-DD.
            MOVE WS-TO-DATE-XX(7:4)      TO WS-TO-DATE-YYYY.
            IF WS-FIND-GREATER
               IF WS-TO-DATE > WS-FROM-DATE
                  MOVE  WS-TO-DATE-XX    TO WS-DATE-ANSWER
               ELSE
                  MOVE  WS-FROM-DATE-XX  TO WS-DATE-ANSWER
               END-IF
            ELSE
               IF WS-TO-DATE < WS-FROM-DATE
                  MOVE  WS-TO-DATE-XX    TO WS-DATE-ANSWER
               ELSE
                  MOVE  WS-FROM-DATE-XX  TO WS-DATE-ANSWER
               END-IF
            END-IF.
       8800-EXIT.
           EXIT.
      **

       9000-LINK-TO-PRICER.
           EXEC CICS LINK
           PROGRAM  (A6U01-PROGRAM)
           COMMAREA (A6U01-PARM-LIST)
           RESP (WS-XCTL-CICS-RESP)
           END-EXEC.

       9000-EXIT.
           EXIT.

       9015-LINK-TO-EXPLODER.
           EXEC CICS LINK
           PROGRAM  (A6W012U-PROGRAM)
           COMMAREA (A6W012U-PARM-LIST)
           RESP (WS-XCTL-CICS-RESP)
           END-EXEC.

       9015-EXIT.
           EXIT.

       9020-LINK-TO-A6O013.
           EXEC CICS LINK
           PROGRAM  (A6W013U-PROGRAM)
           COMMAREA (A6W013U-PARM-LIST)
           RESP (WS-XCTL-CICS-RESP)
           END-EXEC.

       9020-EXIT.
           EXIT.
       9490-SQL-SELECT-VNG02.

           EXEC SQL
                SELECT
                      VNG02.C_VND_PROD_BASE_UM
                INTO
                      :VNG02-C-VND-PROD-BASE-UM
                FROM VNG02
                WHERE VNG02.I_VENDOR = :VNG02-I-VENDOR AND
                      VNG02.I_VND_PRODUCT = :VNG02-I-VND-PRODUCT
           END-EXEC.
           EVALUATE SQLCODE
               WHEN +0
                   CONTINUE
               WHEN OTHER
                   PERFORM 9997-BUILD-ERROR-MSG
                     THRU  9997-EXIT
                   MOVE 'Y'                   TO OMGPR-F-PRICER-ERROR
             END-EVALUATE.


       9490-EXIT.
             EXIT.
      *
       9495-SELECT-VNG05.
           EXEC SQL
                SELECT
                      VNG05.A_VD_PRD_ALT_UMF
                INTO
                      :VNG05-A-VD-PRD-ALT-UMF
                FROM VNG05
                WHERE VNG05.I_VENDOR = :VNG05-I-VENDOR AND
                      VNG05.I_VND_PRODUCT = :VNG05-I-VND-PRODUCT AND
                      VNG05.C_VD_PRD_ALT_UM = :VNG05-C-VD-PRD-ALT-UM
           END-EXEC.
           EVALUATE SQLCODE
               WHEN +0
                   CONTINUE
               WHEN OTHER
                   PERFORM 9996-BUILD-ERROR-MSG
                     THRU  9996-EXIT
                   MOVE 'Y'                   TO OMGPR-F-PRICER-ERROR
             END-EVALUATE.


       9495-EXIT.
             EXIT.
      *
       9996-BUILD-ERROR-MSG.
            IF SQLCODE = 100

               MOVE SQLCODE             TO WS-SQL-ERR-NUM

               MOVE 'ALT UOM  NOT FND' TO WS-SQL-ERROR-MESSAGE
               MOVE WS-SQL-ERR-MSG      TO WS-ERR-MSG
            ELSE
               MOVE SQLCODE             TO WS-SQL-ERR-NUM

               MOVE 'SQL ERR IN VNG05'  TO WS-SQL-ERROR-MESSAGE
               MOVE WS-SQL-ERR-MSG      TO  WS-ERROR-MESSAGE
            END-IF.
       9996-EXIT.
           EXIT.

       9997-BUILD-ERROR-MSG.
            IF SQLCODE = 100

               MOVE SQLCODE             TO WS-SQL-ERR-NUM

               MOVE 'COMPONENT NOT FND' TO WS-SQL-ERROR-MESSAGE
               MOVE WS-SQL-ERR-MSG      TO WS-ERR-MSG
            ELSE
               MOVE SQLCODE             TO WS-SQL-ERR-NUM

               MOVE 'SQL ERR IN VNG02'  TO WS-SQL-ERROR-MESSAGE
               MOVE WS-SQL-ERR-MSG      TO  WS-ERROR-MESSAGE
            END-IF.
       9997-EXIT.
           EXIT.


       9998-BUILD-ERROR-MSG.
            IF A6W13-ERROR-SQLCODE > SPACES
               MOVE A6W13-ERROR-SQLCODE  TO WS-SQL-ERR-NUM
               MOVE A6W13-ERROR-MESSAGE  TO WS-SQL-ERROR-MESSAGE
               MOVE WS-SQL-ERR-MSG       TO WS-ERR-MSG
            ELSE
               MOVE A6W13-GENERAL-ERROR-NUMBER   TO  WS-ERR-NUM
               MOVE A6W13-ERROR-MESSAGE  TO  WS-ERROR-MESSAGE
            END-IF.
       9998-EXIT.
           EXIT.


       9999-BUILD-ERROR-MSG.
            IF OMGEXPL-SQL-CODE > SPACES
               MOVE OMGEXPL-SQL-CODE     TO WS-SQL-ERR-NUM
               MOVE OMGEXPL-ERROR-MSG    TO WS-SQL-ERROR-MESSAGE
               MOVE WS-SQL-ERR-MSG       TO WS-ERR-MSG
            ELSE
               MOVE OMGEXPL-ERROR-NUMBER TO  WS-ERR-NUM
               MOVE OMGEXPL-ERROR-MSG    TO  WS-ERROR-MESSAGE
            END-IF.
       9999-EXIT.
           EXIT.



