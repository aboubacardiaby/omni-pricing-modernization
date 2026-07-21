       IDENTIFICATION DIVISION.
       PROGRAM-ID.                     A6O016U IS INITIAL.
       AUTHOR.                         SATHYA.
      ************************  REMARKS  ***************************
      *    * COMPILE = PN RQ                                      *
      *    * TYPE    = 3                                          *
      **----------------------------------------------------------**
      ** DUAL COMPILER                                            **
      **--------------------------------------------------------- **
      **  PROJECT NUMBER: KITTING                                 **
      **                                                          **
      **  IMPLEMENTATION DATE:  07/31/2003                        **
      **                                                          **
      **  PROGRAM DEVELOPER: SATHYA, DATA DIRECTIONS              **
      **                                                          **
      **  DESCRIPTION:  PRODUCT DAO                               **
      **  PURPOSE :  GET PRODUCT TYPE, BASE UOM, CATEGORY FOR A   **
      **             GIVEN PRODUCT. ALSO THIS OUTPUTS CONV FACTOR **
      **             AN ALTERNATE UOM                             **
      **             REF TO A6R16 COPY BOOK FOR INPUTS & OUTPUTS  **
      **  NOTES:                                                  **
      **  MODIFICATION LOG:                                       **
      **     DATE  PRGM DESCRIPTION                               **
      **  __/__/__ ___  _________________________________________ **
      **************************************************************
       ENVIRONMENT DIVISION.
       DATA DIVISION.
      *****************************************************************
      ***   W O R K I N G     S T O R A G E      S E C T I O N       **
      *****************************************************************
       WORKING-STORAGE SECTION.
       01  WS.
           05  WS-MARKER                    PIC X(14)
                                            VALUE 'A6U16 BEGINS>>'.
           05  WS-TRANSID                   PIC X(4)
                                            VALUE 'A616'.
           05  WS-CONSTANTS.
               10  FILLER                   PIC X(12)
                                            VALUE 'CONSTANTS==>'.
               10  WS-CURRENT-PROGRAM       PIC X(08)
                                            VALUE 'A6U16   '.
               10  WS-CURRENT-TRANSACTION   PIC X(04)
                                            VALUE 'A616'.
               10  WS-ERROR-TYPES.
                   15 WS-ABEND-ERROR        PIC X(01) VALUE 'A'.
                   15 WS-NONABEND-ERROR     PIC X(01) VALUE 'E'.
                   15 WS-WARNING            PIC X(01) VALUE 'W'.
               10  WS-NEXT-ERROR-NBR-X.
                   15  WS-NEXT-ERROR-NBR    PIC 9(03)
                                            VALUE 007.
           05  WS-WORK-AREAS.
               10  FILLER                   PIC X(12)
                                            VALUE 'WORK AREAS=>'.
               10 WS-LENGTH-TO-PASS         PIC S9(4) USAGE COMP SYNC
                                             VALUE ZERO.
               10 WS-EDITED-SQLCODE         PIC -Z999.
      *
       01 WS-WHAT-TO-PASS.
      *
      *    COPY BOOK USED AS PARM FOR THIS SUBROUTINE
           COPY A6R16.
      *
      *    DB2 DEFINITIONS & OTHER INCLUDES
      *
           EXEC SQL
             INCLUDE VNG05
           END-EXEC.
           EXEC SQL
             INCLUDE VNG06
           END-EXEC.
      *
           EXEC SQL
             INCLUDE ING01
           END-EXEC.
      *
           EXEC SQL
             INCLUDE VNG02
           END-EXEC.
           EXEC SQL
               INCLUDE SQLCA
           END-EXEC.
      *
      *****************************************************************
      ***              L I N K A G E          S E C T I O N          **
      *****************************************************************
       LINKAGE SECTION.
       01  DFHCOMMAREA.
           05  COMMAREA                     PIC X(250).
      *
       PROCEDURE DIVISION.
           PERFORM 0010-INIT
             THRU  0010-EXIT.
           PERFORM 0020-PROCESS
             THRU  0020-EXIT.
           PERFORM 0030-EOJ
             THRU  0030-EXIT.
      *
       0010-INIT.
           MOVE 250                      TO WS-LENGTH-TO-PASS.
           MOVE DFHCOMMAREA              TO WS-WHAT-TO-PASS.
           INITIALIZE A6W16-RTND-DATA.
       0010-EXIT.
           EXIT.
      *
       0020-PROCESS.
           PERFORM 1000-VALIDATE-INPUT
             THRU  1000-EXIT.
           IF A6W16-ABEND OR
              A6W16-ERROR
              GO TO 0020-EXIT
           END-IF.
           PERFORM 2200-READ-PRODUCT-DATA
             THRU  2200-EXIT.
           IF A6W16-ABEND OR
              A6W16-ERROR
              GO TO 0020-EXIT
           END-IF.
           IF A6W16-GET-PROD-INFO
      * SINCE ONLY PRODUCT INFO ( PRODUCT TYPE) IS REQUIRED, NO NEED
      * TO GO FURTHER
              GO TO 0020-EXIT
           END-IF.
           PERFORM 2000-FIND-CAT
             THRU  2000-EXIT.
           IF A6W16-ABEND OR
              A6W16-ERROR
              GO TO 0020-EXIT
           END-IF.
           IF A6W16-BRANCH > SPACES
              PERFORM 3000-FIND-INV-CLASS
                THRU  3000-EXIT
              IF A6W16-ABEND OR
                 A6W16-ERROR
                   GO TO 0020-EXIT
              END-IF
           END-IF.
           IF A6W16-UOM = SPACES
              CONTINUE
           ELSE
             IF A6W16-UOM = A6W16-BASE-UOM
                MOVE 1                   TO A6W16-CONV-FACTOR
             ELSE
               PERFORM 2500-FIND-CONV-FACTOR
                 THRU  2500-EXIT
               IF A6W16-ABEND OR
                  A6W16-ERROR
                 GO TO 0020-EXIT
               END-IF
             END-IF
           END-IF.
       0020-EXIT.
           EXIT.
      *
      *
       0030-EOJ.
      *
           MOVE A6W16                    TO DFHCOMMAREA.
           PERFORM 8000-PROCESS-RETURN
             THRU  8000-EXIT.
       0030-EXIT.
           EXIT.
      *
       1000-VALIDATE-INPUT.
           IF A6W16-I-VENDOR = SPACES
              MOVE WS-NONABEND-ERROR     TO A6W16-GENERAL-RESPONSE-FLAG
              MOVE '61601- VENDOR# NOT PROVIDED'
                                         TO A6W16-ERROR-MESSAGE
           END-IF.
           IF A6W16-ABEND OR
              A6W16-ERROR
              GO TO 1000-EXIT
           END-IF.
           IF A6W16-I-VND-PRODUCT = SPACES
              MOVE WS-NONABEND-ERROR     TO A6W16-GENERAL-RESPONSE-FLAG
              MOVE '61604- PROD# NOT PROVIDED'
                                         TO A6W16-ERROR-MESSAGE
           END-IF.
       1000-EXIT.
           EXIT.
      *
       2000-FIND-CAT.
           MOVE A6W16-I-VENDOR           TO VNG06-I-VENDOR.
           MOVE A6W16-I-VND-PRODUCT      TO VNG06-I-VND-PRODUCT.
           PERFORM 9025-SELECT-VNG06
             THRU  9025-EXIT.
           EVALUATE SQLCODE
               WHEN +0
                   MOVE VNG06-S-PROD-CATEGORY
                                         TO A6W16-S-PROD-CATEGORY
                   MOVE SPACES           TO
                                           A6W16-GENERAL-RESPONSE-FLAG
                   IF VNG06-D-CAT-PROD-EXPIRE-NI < 0
                      MOVE SPACES        TO VNG06-D-CAT-PROD-EXPIRE
                   END-IF
                   MOVE VNG06-D-CAT-PROD-EXPIRE
                                         TO A6W16-PROD-CAT-EXP-DATE
                   MOVE VNG06-D-CAT-PROD-EFFECT
                                         TO A6W16-PROD-CAT-EFF-DATE
               WHEN 100
                   MOVE WS-WARNING       TO
                          A6W16-GENERAL-RESPONSE-FLAG
                   MOVE '61605- PRODUCT CATEGORY NOT FOUND '
                                         TO A6W16-ERROR-MESSAGE
                   MOVE SQLCODE          TO WS-EDITED-SQLCODE
                   MOVE WS-EDITED-SQLCODE
                                         TO A6W16-ERROR-SQLCODE
               WHEN OTHER
                   MOVE WS-ABEND-ERROR   TO A6W16-GENERAL-RESPONSE-FLAG
                   MOVE '61606- SQL ERROR IN SELCT VNG06'
                                         TO A6W16-ERROR-MESSAGE
                   MOVE SQLCODE          TO WS-EDITED-SQLCODE
                   MOVE WS-EDITED-SQLCODE
                                         TO A6W16-ERROR-SQLCODE
           END-EVALUATE.

       2000-EXIT.
           EXIT.
      *
       2200-READ-PRODUCT-DATA.
           MOVE A6W16-I-VENDOR           TO VNG02-I-VENDOR.
           MOVE A6W16-I-VND-PRODUCT      TO VNG02-I-VND-PRODUCT.
           PERFORM 9015-SELECT-VNG02
             THRU  9015-EXIT.
           EVALUATE SQLCODE
               WHEN +0
                   EVALUATE VNG02-C-PRODUCT-TYPE
                     WHEN 'O'
      * OWENS KIT
                         MOVE VNG02-C-PRODUCT-TYPE
                                         TO A6W16-PROD-TYPE
                     WHEN 'S'
      * SUPPLIER KIT
                         MOVE VNG02-C-PRODUCT-TYPE
                                         TO A6W16-PROD-TYPE
                     WHEN OTHER
      * INCLUDING BLANKS, IT MEANS REGULAR PRODUCTS
                         MOVE 'R'
                                         TO A6W16-PROD-TYPE
                   END-EVALUATE

                   MOVE VNG02-C-VND-PROD-BASE-UM
                                         TO A6W16-BASE-UOM
                   MOVE SPACES           TO A6W16-GENERAL-RESPONSE-FLAG
               WHEN 100
                   MOVE WS-NONABEND-ERROR
                                         TO A6W16-GENERAL-RESPONSE-FLAG
                   MOVE '61603- PRODUCT NOT FOUND '
                                         TO A6W16-ERROR-MESSAGE
                   MOVE SQLCODE          TO WS-EDITED-SQLCODE

                   MOVE WS-EDITED-SQLCODE
                                         TO A6W16-ERROR-SQLCODE
               WHEN OTHER
                   MOVE WS-ABEND-ERROR   TO A6W16-GENERAL-RESPONSE-FLAG
                   MOVE '61602- SQL ERROR IN SELCT VNG02'
                                         TO A6W16-ERROR-MESSAGE
                   MOVE SQLCODE          TO WS-EDITED-SQLCODE

                   MOVE WS-EDITED-SQLCODE
                                         TO A6W16-ERROR-SQLCODE
           END-EVALUATE.
       2200-EXIT.
           EXIT.
      *
       2500-FIND-CONV-FACTOR.
           MOVE A6W16-I-VENDOR           TO VNG05-I-VENDOR.
           MOVE A6W16-I-VND-PRODUCT      TO VNG05-I-VND-PRODUCT.
           MOVE A6W16-UOM                TO VNG05-C-VD-PRD-ALT-UM.
           PERFORM 9050-SELECT-VNG05
             THRU  9050-EXIT.
           EVALUATE SQLCODE
               WHEN +0
                   MOVE VNG05-A-VD-PRD-ALT-UMF
                                         TO A6W16-CONV-FACTOR
                   MOVE SPACES           TO A6W16-GENERAL-RESPONSE-FLAG
               WHEN 100
                   MOVE WS-NONABEND-ERROR
                                         TO A6W16-GENERAL-RESPONSE-FLAG
                   MOVE '61606- UOM NOT FOUND '
                                         TO A6W16-ERROR-MESSAGE
                   MOVE SQLCODE          TO WS-EDITED-SQLCODE
                   MOVE WS-EDITED-SQLCODE
                                         TO A6W16-ERROR-SQLCODE
               WHEN OTHER
                   MOVE WS-ABEND-ERROR   TO A6W16-GENERAL-RESPONSE-FLAG
                   MOVE '61607- SQL ERROR IN SELCT VNG05'
                                         TO A6W16-ERROR-MESSAGE
                   MOVE SQLCODE          TO WS-EDITED-SQLCODE
                   MOVE WS-EDITED-SQLCODE
                                         TO A6W16-ERROR-SQLCODE
           END-EVALUATE.

       2500-EXIT.
           EXIT.
      *
       3000-FIND-INV-CLASS.
           MOVE A6W16-I-VENDOR           TO ING01-I-VENDOR.
           MOVE A6W16-I-VND-PRODUCT      TO ING01-I-VND-PRODUCT.
           MOVE A6W16-BRANCH             TO ING01-I-DIVISION.
           PERFORM 9030-SELECT-ING01
             THRU  9030-EXIT.
           EVALUATE SQLCODE
               WHEN +0
                   MOVE ING01-C-INV-CLASS TO A6W16-C-INV-CLASS
               WHEN 100
                   CONTINUE
               WHEN OTHER
                   MOVE WS-ABEND-ERROR   TO A6W16-GENERAL-RESPONSE-FLAG
                   MOVE '61607- SQL ERROR IN SELCT VNG06'
                                         TO A6W16-ERROR-MESSAGE
                   MOVE SQLCODE          TO WS-EDITED-SQLCODE
                   MOVE WS-EDITED-SQLCODE
                                         TO A6W16-ERROR-SQLCODE
           END-EVALUATE.

       3000-EXIT.
           EXIT.
      *
       8000-PROCESS-RETURN.
           EXEC CICS RETURN END-EXEC.
           GOBACK.
       8000-EXIT.
           EXIT.
      *
       9015-SELECT-VNG02.

           EXEC SQL
                SELECT  VNG02.C_PRODUCT_TYPE
                      , VNG02.C_VND_PROD_BASE_UM
                INTO :VNG02-C-PRODUCT-TYPE
                    ,:VNG02-C-VND-PROD-BASE-UM
                FROM VNG02
                WHERE VNG02.I_VENDOR      = :VNG02-I-VENDOR   AND
                      VNG02.I_VND_PRODUCT = :VNG02-I-VND-PRODUCT
           END-EXEC.
       9015-EXIT.
           EXIT.
      *
       9025-SELECT-VNG06.
           EXEC SQL
                SELECT
                      VNG06.S_PROD_CATEGORY
MFMIGR               ,CONVERT(CHAR(10),VNG06.D_CAT_PROD_EFFECT,101)
MFMIGR               ,CONVERT(CHAR(10),VNG06.D_CAT_PROD_EXPIRE,101)
                INTO
                     :VNG06-S-PROD-CATEGORY
                    ,:VNG06-D-CAT-PROD-EFFECT
                    ,:VNG06-D-CAT-PROD-EXPIRE
                     :VNG06-D-CAT-PROD-EXPIRE-NI
                FROM VNG06
                WHERE VNG06.I_VENDOR      = :VNG06-I-VENDOR   AND
                      VNG06.I_VND_PRODUCT = :VNG06-I-VND-PRODUCT
           END-EXEC.
       9025-EXIT.
           EXIT.
      *
       9030-SELECT-ING01.
              EXEC SQL
                   SELECT
                         ING01.C_DIV_INV_PRC_LVL,
                         ING01.F_STOCK_ITEM,
                         ING01.A_DIV_INV_FREIGHT,
                         ING01.C_INV_CLASS,
                         ING01.C_INV_SUB_CLASS
                   INTO
                         :ING01-C-DIV-INV-PRC-LVL,
                         :ING01-F-STOCK-ITEM,
                         :ING01-A-DIV-INV-FREIGHT,
                         :ING01-C-INV-CLASS,
                         :ING01-C-INV-SUB-CLASS
                   FROM ING01
             WHERE      I_DIVISION    = :ING01-I-DIVISION AND
                        I_VENDOR      = :ING01-I-VENDOR   AND
                        I_VND_PRODUCT = :ING01-I-VND-PRODUCT
           END-EXEC.
       9030-EXIT.
           EXIT.
      *
       9050-SELECT-VNG05.
           EXEC SQL
                SELECT
                      VNG05.A_VD_PRD_ALT_UMF
                INTO
                     :VNG05-A-VD-PRD-ALT-UMF
                FROM VNG05
                WHERE VNG05.I_VENDOR      =   :VNG05-I-VENDOR      AND
                      VNG05.I_VND_PRODUCT =   :VNG05-I-VND-PRODUCT AND
                      VNG05.C_VD_PRD_ALT_UM = :VNG05-C-VD-PRD-ALT-UM
           END-EXEC.
       9050-EXIT.
           EXIT.
      *
