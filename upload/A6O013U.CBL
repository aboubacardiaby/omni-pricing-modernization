       IDENTIFICATION DIVISION.
       PROGRAM-ID.                     A6O013U IS INITIAL.
       AUTHOR.                         SATHYA.
      ************************  REMARKS  ***************************
      *    * COMPILE = PN RQ                                      *
      *    * TYPE    = 3                                          *
      **                                                          **
      **  PROJECT NUMBER: KITTING                                 **
      **                                                          **
      **  IMPLEMENTATION DATE:  07/31/2003                        **
      **                                                          **
      **  PROGRAM DEVELOPER: SATHYA                               **
      **                                                          **
      **  DESCRIPTION:  ONLINE COMPONENT TO GET CATEGORY & PRICE  **
      **                LIST FOR A PRODUCT                        **
      **  NOTES:                                                  **
      **  Modification Log:                                       **
      **     DATE  PRGM DESCRIPTION                               **
      **  __/__/__ ___  _________________________________________ **
MFMIGR*----------------------------------------------------------------*
MFMIGR* 04/08/06  CR050057   JIM NOLAN   MODIFIED SET STATEMENT TO     *
MFMIGR*           MFMIGR                 RETRIEVE CURRENT DATE IN ORDER*
MFMIGR*                                  TO WORK WITH ANSI COMPLIANT   *
MFMIGR*                                  DBRM DATABASES.               *
MFMIGR*----------------------------------------------------------------*
      **************************************************************
       ENVIRONMENT DIVISION.
       DATA DIVISION.
      *****************************************************************
      ***   W O R K I N G     S T O R A G E      S E C T I O N       **
      *****************************************************************
       WORKING-STORAGE SECTION.
       01  WS.
           05  WS-MARKER                    PIC X(14)
                                            VALUE 'A6U13 BEGINS>>'.
           05  WS-TRANSID                   PIC X(4)
                                            VALUE 'A613'.
           05  WS-CONSTANTS.
               10  FILLER                   PIC X(12)
                                            VALUE 'CONSTANTS==>'.
               10  WS-CURRENT-PROGRAM       PIC X(08)
                                            VALUE 'A6U13   '.
               10  WS-CURRENT-TRANSACTION   PIC X(04)
                                            VALUE 'A613'.
               10  A6W016U-PROGRAM       PIC X(08)
                                                 VALUE 'A6O016U'.
               10  WS-GET-CATEGORY          PIC X(01)      VALUE 'C'.
               10  WS-ERROR-TYPES.
                   15 WS-ABEND-ERROR        PIC X(01) VALUE 'A'.
                   15 WS-NONABEND-ERROR     PIC X(01) VALUE 'E'.
                   15 WS-WARNING            PIC X(01) VALUE 'W'.
               10  WS-NEXT-ERROR-NBR-X.
                   15  WS-NEXT-ERROR-NBR    PIC 9(03)
                                            VALUE 003.
           05  WS-WORK-AREAS.
               10  FILLER                   PIC X(12)
                                            VALUE 'WORK AREAS=>'.
               10 WS-LENGTH-TO-PASS         PIC S9(4) USAGE COMP SYNC
                                             VALUE ZERO.
               10  WS-CURRENT-DATE          PIC X(10).
               10 WS-EDITED-SQLCODE         PIC -Z999.
               10 WS-XCTL-CICS-RESP-X.
                   15 WS-XCTL-CICS-RESP     PIC S9(8)
                                            VALUE     ZERO.
       01 WS-WHAT-TO-PASS.
                       COPY A6R13.
       01  A6W016U-PARM-LIST.
              COPY A6R16.
           EXEC SQL
                INCLUDE DFHRESP
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
           05  COMMAREA                     PIC X(250).
       PROCEDURE DIVISION.
           PERFORM 0010-INIT
             THRU  0010-EXIT.
           PERFORM 0020-PROCESS
             THRU  0020-EXIT.
           PERFORM 0030-EOJ
             THRU  0030-EXIT.

       0010-INIT.
           MOVE 250                      TO WS-LENGTH-TO-PASS.
           MOVE DFHCOMMAREA              TO WS-WHAT-TO-PASS.
           INITIALIZE A6W13-RTND-DATA.
           INITIALIZE A6W16.
            EXEC SQL
MFMIGR         SELECT CONVERT(CHAR(10),CURRENT_TIMESTAMP,101)
MFMIGR                INTO :WS-CURRENT-DATE
MFMIGR
            END-EXEC.
       0010-EXIT.
           EXIT.

       0020-PROCESS.
           PERFORM 1000-VALIDATE-INPUT
             THRU  1000-EXIT.
           IF A6W13-ABEND OR
              A6W13-ERROR
              GO TO 0020-EXIT
           END-IF.
           PERFORM 2000-READ-PRODUCT-DATA
             THRU  2000-EXIT.
           IF A6W13-ABEND OR
              A6W13-ERROR
              GO TO 0020-EXIT
           END-IF.
           MOVE A6W16-GENERAL-RESPONSE-FLAG
                                         TO A6W13-GENERAL-RESPONSE-FLAG.
           MOVE A6W16-GENERAL-ERROR-NUMBER
                                         TO A6W13-GENERAL-ERROR-NUMBER.
           MOVE A6W16-ABEND-MSG          TO A6W13-ABEND-MSG.
           MOVE A6W16-S-PROD-CATEGORY
                                         TO A6W13-S-PROD-CATEGORY.
           MOVE A6W16-BASE-UOM           TO A6W13-BASE-UOM.
           MOVE A6W16-CONV-FACTOR        TO A6W13-CONV-FACTOR.
           MOVE A6W16-C-INV-CLASS        TO A6W13-INV-CLASS
           MOVE A6W16-PROD-CAT-EFF-DATE
                                         TO A6W13-PROD-CAT-EFF-DATE.
           MOVE A6W16-PROD-CAT-EXP-DATE
                                         TO A6W13-PROD-CAT-EXP-DATE.
       0020-EXIT.
           EXIT.
      *************************
       0030-EOJ.
      *************************
           MOVE A6W13                    TO DFHCOMMAREA.
           PERFORM 8000-PROCESS-RETURN
             THRU  8000-EXIT.


       0030-EXIT.
           EXIT.

       1000-VALIDATE-INPUT.
           IF A6W13-I-VENDOR = SPACES
              MOVE WS-NONABEND-ERROR     TO A6W13-GENERAL-RESPONSE-FLAG
              MOVE '61301- VENDOR# NOT PROVIDED'
                                         TO A6W13-ERROR-MESSAGE
           END-IF.
           IF A6W13-ABEND OR
              A6W13-ERROR
              GO TO 1000-EXIT
           END-IF.
           IF A6W13-I-VND-PRODUCT = SPACES
              MOVE WS-NONABEND-ERROR     TO A6W13-GENERAL-RESPONSE-FLAG
              MOVE '61304- PROD# NOT PROVIDED'
                                         TO A6W13-ERROR-MESSAGE
           END-IF.
       1000-EXIT.
           EXIT.
       2000-READ-PRODUCT-DATA.
           MOVE A6W13-I-VENDOR           TO A6W16-I-VENDOR
           MOVE A6W13-I-VND-PRODUCT      TO A6W16-I-VND-PRODUCT
           MOVE A6W13-UOM                TO A6W16-UOM.
           MOVE A6W13-BRANCH             TO A6W16-BRANCH.
           MOVE WS-GET-CATEGORY          TO A6W16-REQUIREMENT.
           PERFORM 9015-GET-PRODUCT-DAO
             THRU  9015-EXIT.
           EVALUATE TRUE
               WHEN WS-XCTL-CICS-RESP =
                    DFHRESP-NORMAL
                   CONTINUE
               WHEN WS-XCTL-CICS-RESP =
                    DFHRESP-PGMIDERR
                 MOVE
                    'A6U16 TRANSACTION IS UNAVAILABLE'
                     TO A6W13-ERROR-MESSAGE
                   MOVE WS-NONABEND-ERROR
                                         TO A6W13-GENERAL-RESPONSE-FLAG

               WHEN OTHER
                    MOVE  'CICS LINK ERR '
                                         TO A6W13-ERROR-MESSAGE
                    MOVE WS-NONABEND-ERROR
                                         TO A6W13-GENERAL-RESPONSE-FLAG
                    MOVE WS-XCTL-CICS-RESP
                                         TO WS-EDITED-SQLCODE
                    MOVE WS-EDITED-SQLCODE
                                         TO A6W13-ERROR-SQLCODE
            END-EVALUATE.
       2000-EXIT.
            EXIT.
       8000-PROCESS-RETURN.
           EXEC CICS
                RETURN
           END-EXEC.
           GOBACK.
       8000-EXIT.
           EXIT.

       9015-GET-PRODUCT-DAO.
           EXEC CICS LINK
           PROGRAM  (A6W016U-PROGRAM)
           COMMAREA (A6W016U-PARM-LIST)
           RESP (WS-XCTL-CICS-RESP)
           END-EXEC.
       9015-EXIT.
           EXIT.
