      *----------------------------------------------------------------
      *    08/16/21 SJACOB   IR 20-0065 PRICER ENHANCEMENT PHASE 2     
      *                      COPYBOOK FOR TABLE HC_OVRD_GROUP_ACCOUNT  
      *                      INITIAL CREATION                          
      *----------------------------------------------------------------
       01 HC-OVD-ACCOUNT.                                                
          05 HC-ACT-GROUP-ID           PIC S9(8) VALUE 0 COMPUTATIONAL.
          05 HC-ACT-DIVISION           PIC X(02) VALUE SPACE.          
          05 HC-ACT-ACCOUNT            PIC X(06) VALUE SPACE.          
          05 HC-ACT-START-DATE         PIC X(23) VALUE SPACE.          
          05 HC-ACT-EXPIRE-DATE        PIC X(23) VALUE SPACE.          
          05 HC-ACT-USER-ID            PIC X(08) VALUE SPACE.
                                                                       
       01  HC-ACT-NULL-INDICATORS.                                   
              10 HC-ACT-EXP-DATE-NI    PIC S9(4) VALUE 0 COMPUTATIONAL.