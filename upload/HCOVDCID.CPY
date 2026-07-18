      *----------------------------------------------------------------
      *    08/16/21 SJACOB   IR 20-0065 PRICER ENHANCEMENT PHASE 2     
      *                      COPYBOOK FOR TABLE HC_OVRD_GROUP_CUSTOMER 
      *                      INITIAL CREATION                          
      *----------------------------------------------------------------
       01 HC-OVD-CUSTOMER.                                                
          05 HC-CID-GROUP-ID           PIC S9(8) VALUE 0 COMPUTATIONAL.
          05 HC-CID-CUST-ID            PIC S9(10) VALUE 0 COMP-3.      
          05 HC-CID-START-DATE         PIC X(23) VALUE SPACE.          
          05 HC-CID-EXPIRE-DATE        PIC X(23) VALUE SPACE.          
          05 HC-CID-USER-ID            PIC X(08) VALUE SPACE.
                                                                       
       01  HC-CID-NULL-INDICATORS.                                   
              10 HC-CID-EXP-DATE-NI    PIC S9(4) VALUE 0 COMPUTATIONAL.