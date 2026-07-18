      *****************************************************************
      * 01/01/2014 NKIMBO   PRICING STRATEGY :  WEB 
      *                    CLIENT FIELDS FOR NDP.
      *****************************************************************
           05  A6W001-PARM-LIST.
              10  A6W001-PROGRAM             PIC X(08)  VALUE SPACES.
              10  A6W001-CALLING-PROGRAM-ID
                                             PIC X(08)  VALUE SPACES.
              10  A6W001-PARM-LENGTH         PIC 9(5)   VALUE 831.
              10  A6W001-MODE                PIC X(01)  VALUE SPACE.
              10  A6W001-SELECT-FIELDS.
                  15  A6W001-DC-ACCT-NO      PIC X(08)  VALUE SPACES.
                  15  A6W001-SHIP-TO         PIC X(03)  VALUE SPACES.   
                  15  A6W001-VENDOR-PRODUCT  PIC X(12)  VALUE SPACES.
                  15  A6W001-UNIT-OF-MEASURE PIC X(02)  VALUE SPACES.
                  15  A6W001-PRICING-DATE. 
                      20 A6W001-PRICING-CCYY PIC X(04)  VALUE SPACES.
                      20 A6W001-PRICING-MM   PIC X(02)  VALUE SPACES.
                      20 A6W001-PRICING-DD   PIC X(02)  VALUE SPACES.
  ******   OUTPUT FROM WEB SERVICE
              10  A6W001-WEB-AGREEMENT-ID      PIC S9(9)  VALUE +0.
              10  A6W001-WEB-AGREEMENT-NAME    PIC X(100) VALUE SPACES.
              10  A6W001-WEB-CURRENTCPLUSPCT   PIC S9(9)V9(9) VALUE +0.
              10  A6W001-WEB-ERROR-APP         PIC X(03) VALUE SPACES.
              10  A6W001-WEB-EXPIRATEDATE      PIC X(100) VALUE SPACES.
              10  A6W001-WEB-GPPCT             PIC S9(9)V9(9) VALUE +0.
              10  A6W001-WEB-NDP               PIC S9(9)V9(9) VALUE +0.
              10  A6W001-WEB-PRICELISTNAME     PIC X(100) VALUE SPACES.
              10  A6W001-WEB-SELLGROUPINFO     PIC X(100) VALUE SPACES.
              10  A6W001-WEB-SELLGROUPNAME     PIC X(100) VALUE SPACES.
              10  A6W001-WEB-SELLGROUPTYPE     PIC X(100) VALUE SPACES.
              10  A6W001-WEB-ERROR             PIC X(01)  VALUE SPACES.
              10  A6W001-WEB-ERROR-NBR         PIC S9(9)  VALUE +0.
              10  A6W001-WEB-ERROR-MSG         PIC X(100) VALUE SPACES.

