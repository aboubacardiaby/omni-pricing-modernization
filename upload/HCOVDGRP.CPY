      *----------------------------------------------------------------
      *    08/16/21 SJACOB   IR 20-0065 PRICER ENHANCEMENT PHASE 2     
      *                      COPYBOOK FOR TABLE HC_OVRD_PROD_GROUP.    
      *                      INITIAL CREATION                          
      *----------------------------------------------------------------
       01 HC-OVD-GROUP.                                             
          05 HC-GROUP-ID               PIC S9(8) VALUE 0 COMPUTATIONAL.
          05 HC-GROUP-COMMENT          PIC X(35) VALUE SPACE.          
          05 HC-GROUP-HDR-START-DATE   PIC X(23) VALUE SPACE.          
          05 HC-GROUP-HDR-EXPIRE-DATE  PIC X(23) VALUE SPACE.          
          05 HC-GROUP-HDR-USER-ID      PIC X(08) VALUE SPACE.
                                                                       
       01  HC-GROUP-NULL-INDICATORS.                                   
              10 HC-GROUP-HDR-EXP-DATE-NI                              
                                       PIC S9(4) VALUE 0 COMPUTATIONAL.
