      ******************************************************************
      * DCLGEN TABLE(U1.CC_CNTSHPTO_EXCLSN)                            *
      *        LIBRARY(TEST.SATHYA.COPYLIB(CCG25))                     *
      *        LANGUAGE(COBOL)                                         *
      *        STRUCTURE(CCG25)                                        *
      *        QUOTE                                                   *
      *        LABEL(YES)                                              *
      *        INDVAR(YES)                                             *
      * ... IS THE DCLGEN COMMAND THAT MADE THE FOLLOWING STATEMENTS   *
      * INSTALLED BY: SATHYA 04/15/2004                                *
      * CR03-0150                                                      *
      ******************************************************************
           EXEC SQL DECLARE CCG25  TABLE
           ( I_CONTRACT                     INTEGER NOT NULL,
             I_ACCOUNT                      INTEGER NOT NULL,
             C_SHIP_TO_SUFFIX               CHAR(3) NOT NULL,
             D_EFFECT                       DATE NOT NULL,
             D_EXPIRE                       DATE,
             T_COMMENT                      CHAR(35) NOT NULL,
             C_USERID_EFFECT                CHAR(8) NOT NULL,
             C_USERID_EXPIRE                CHAR(8) NOT NULL,
             C_CREATEDBY_PROC               CHAR(8) NOT NULL,
             C_UPDATEDBY_PROC               CHAR(8) NOT NULL,
             C_CREATED_TIMESTMP             TIMESTAMP NOT NULL,
             C_LASTUPD_TIMESTMP             TIMESTAMP NOT NULL
           ) END-EXEC.
      ******************************************************************
      * COBOL DECLARATION FOR TABLE U1.CC_CNTSHPTO_EXCLSN              *
      ******************************************************************
       01  CCG25.
           05 CCG25-C.
      *    *************************************************************
           10 CCG25-I-CONTRACT           PIC S9(9) USAGE COMP.
      *    *************************************************************
           10 CCG25-I-ACCOUNT            PIC S9(9) USAGE COMP.
      *    *************************************************************
           10 CCG25-C-SHIP-TO-SUFFIX     PIC X(3).
      *    *************************************************************
           10 CCG25-D-EFFECT             PIC X(10).
      *    *************************************************************
           10 CCG25-D-EXPIRE             PIC X(10).
      *    *************************************************************
           10 CCG25-T-COMMENT            PIC X(35).
      *    *************************************************************
           10 CCG25-C-USERID-EFFECT      PIC X(8).
      *    *************************************************************
           10 CCG25-C-USERID-EXPIRE      PIC X(8).
      *    *************************************************************
           10 CCG25-C-CREATEDBY-PROC     PIC X(8).
      *    *************************************************************
           10 CCG25-C-UPDATEDBY-PROC     PIC X(8).
      *    *************************************************************
           10 CCG25-C-CREATED-TIMESTMP   PIC X(26).
      *    *************************************************************
           10 CCG25-C-LASTUPD-TIMESTMP   PIC X(26).
      ******************************************************************
      * INDICATOR VARIABLE STRUCTURE                                   *
      ******************************************************************
       01  CCG25-NULL-INDICATORS.

             10 CCG25-D-EXPIRE-NI                    PIC S9(4)
                                          VALUE     ZERO
                                          COMPUTATIONAL.
      ******************************************************************
      * THE NUMBER OF COLUMNS DESCRIBED BY THIS DECLARATION IS 12      *
      ******************************************************************
