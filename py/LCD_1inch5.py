
import time
from . import lcdconfig

class LCD_1inch5(lcdconfig.RaspberryPi):

    width = 240
    height = 280 
    def command(self, cmd):
        self.digital_write(self.DC_PIN, self.GPIO.LOW)
        self.spi_writebyte([cmd])	
        
    def data(self, val):
        self.digital_write(self.DC_PIN, self.GPIO.HIGH)
        self.spi_writebyte([val])	
        
    def reset(self):
        """Reset the display"""
        self.GPIO.output(self.RST_PIN,self.GPIO.HIGH)
        time.sleep(0.01)
        self.GPIO.output(self.RST_PIN,self.GPIO.LOW)
        time.sleep(0.01)
        self.GPIO.output(self.RST_PIN,self.GPIO.HIGH)
        time.sleep(0.01)
        
    def Init(self):
        """Initialize dispaly"""  
        self.module_init()
        self.reset()

        self.command(0x36)
        self.data(0x00)                 #self.data(0x00)

        self.command(0xfd) 
        self.data(0x06) 
        self.data(0x08) 

        self.command(0x61) 
        self.data(0x07) 
        self.data(0x04) 

        self.command(0x62) 
        self.data(0x00) #00
        self.data(0x44) #44
        self.data(0x45) #40  47

        self.command(0x63) #
        self.data(0x41) #
        self.data(0x07) #
        self.data(0x12) #
        self.data(0x12) #

        self.command(0x64) #
        self.data(0x37) #
	#VSP
        self.command(0x65) #Pump1=4.7MHz #PUMP1 VSP
        self.data(0x09) #D6-5:pump1_clk[1:0] clamp 28 2b
        self.data(0x10) #6.26
        self.data(0x21) 
	#VSN
        self.command(0x66)  #pump=2 AVCL
        self.data(0x09)  #clamp 08 0b 09
        self.data(0x10)  #10
        self.data(0x21) 
	#add source_neg_time
        self.command(0x67) #pump_sel
        self.data(0x21) #21 20
        self.data(0x40) 

	#gamma vap/van
        self.command(0x68) #gamma vap/van
        self.data(0x90) #
        self.data(0x4c) #
        self.data(0x50) #VCOM  
        self.data(0x70) #

        self.command(0xb1) #frame rate
        self.data(0x0F) #0x0f fr_h[5:0] 0F
        self.data(0x02) #0x02 fr_v[4:0] 02
        self.data(0x01) #0x04 fr_div[2:0] 04

        self.command(0xB4) 
        self.data(0x01)  #01:1dot 00:column
	##porch
        self.command(0xB5) 
        self.data(0x02) #0x02 vfp[6:0]
        self.data(0x02) #0x02 vbp[6:0]
        self.data(0x0a) #0x0A hfp[6:0]
        self.data(0x14) #0x14 hbp[6:0]

        self.command(0xB6) 
        self.data(0x04) #
        self.data(0x01) #
        self.data(0x9f) #
        self.data(0x00) #
        self.data(0x02) #
        ##gamme sel
        self.command(0xdf) #
        self.data(0x11) #gofc_gamma_en_sel=1
        ##gamma_test1 A1#_wangly
        #3030b_gamma_new_
        #GAMMA---------------------------------######/

        #GAMMA---------------------------------######/
        self.command(0xE2) 	
        self.data(0x03) #vrp0[5:0]	V0 03
        self.data(0x00) #vrp1[5:0]	V1 
        self.data(0x00) #vrp2[5:0]	V2 
        self.data(0x30) #vrp3[5:0]	V61 
        self.data(0x33) #vrp4[5:0]	V62 
        self.data(0x3f) #vrp5[5:0]	V63

        self.command(0xE5) 	
        self.data(0x3f) #vrn0[5:0]	V63
        self.data(0x33) #vrn1[5:0]	V62	
        self.data(0x30) #vrn2[5:0]	V61 
        self.data(0x00) #vrn3[5:0]	V2 
        self.data(0x00) #vrn4[5:0]	V1 
        self.data(0x03) #vrn5[5:0]  V0 03

        self.command(0xE1) 	
        self.data(0x05) #prp0[6:0]	V15
        self.data(0x67) #prp1[6:0]	V51 

        self.command(0xE4) 	
        self.data(0x67) #prn0[6:0]	V51 
        self.data(0x06) #prn1[6:0]  V15

        self.command(0xE0) 
        self.data(0x05) #pkp0[4:0]	V3 
        self.data(0x06) #pkp1[4:0]	V7  
        self.data(0x0A) #pkp2[4:0]	V21
        self.data(0x0C) #pkp3[4:0]	V29 
        self.data(0x0B) #pkp4[4:0]	V37 
        self.data(0x0B) #pkp5[4:0]	V45 
        self.data(0x13) #pkp6[4:0]	V56 
        self.data(0x19) #pkp7[4:0]	V60 

        self.command(0xE3) 	
        self.data(0x18) #pkn0[4:0]	V60 
        self.data(0x13) #pkn1[4:0]	V56 
        self.data(0x0D) #pkn2[4:0]	V45 
        self.data(0x09) #pkn3[4:0]	V37 
        self.data(0x0B) #pkn4[4:0]	V29 
        self.data(0x0B) #pkn5[4:0]	V21 
        self.data(0x05) #pkn6[4:0]	V7  
        self.data(0x06) #pkn7[4:0]	V3 
        #GAMMA---------------------------------######/

        #source
        self.command(0xE6) 
        self.data(0x00) 
        self.data(0xff) #SC_EN_START[7:0] f0

        self.command(0xE7) 
        self.data(0x01) #CS_START[3:0] 01
        self.data(0x04) #scdt_inv_sel cs_vp_en
        self.data(0x03) #CS1_WIDTH[7:0] 12
        self.data(0x03) #CS2_WIDTH[7:0] 12
        self.data(0x00) #PREC_START[7:0] 06
        self.data(0x12) #PREC_WIDTH[7:0] 12

        self.command(0xE8)  #source
        self.data(0x00)  #VCMP_OUT_EN 81-
        self.data(0x70)  #chopper_sel[6:4]
        self.data(0x00)  #gchopper_sel[6:4] 60
        ##gate
        self.command(0xEc) 
        self.data(0x52) #52

        self.command(0xF1) 
        self.data(0x01) #te_pol tem_extend 00 01 03
        self.data(0x01) 
        self.data(0x02) 


        self.command(0xF6) 
        self.data(0x01) 
        self.data(0x30) 
        self.data(0x00) #
        self.data(0x00) #40 

        self.command(0xfd) 
        self.data(0xfa) 
        self.data(0xfc) 

        self.command(0x3a) 
        self.data(0x55) #

        self.command(0x35) 
        self.data(0x00) 
        
        self.command(0x21)

        self.command(0x11)

        self.command(0x29)
  
    def SetWindows(self, Xstart, Ystart, Xend, Yend):
        #set the X coordinates
        Ystart = Ystart + 20
        Yend = Yend + 20

        self.command(0x2A)
        self.data((Xstart)>>8& 0xff)               #Set the horizontal starting point to the high octet
        self.data((Xstart)   & 0xff)      #Set the horizontal starting point to the low octet
        self.data((Xend-1)>>8& 0xff)        #Set the horizontal end to the high octet
        self.data((Xend-1)   & 0xff) #Set the horizontal end to the low octet 
        
        #set the Y coordinates
        self.command(0x2B)
        self.data((Ystart)>>8& 0xff)
        self.data((Ystart)   & 0xff)
        self.data((Yend-1)>>8& 0xff)
        self.data((Yend-1)   & 0xff)

        self.command(0x2C) 
        
    def ShowImage(self,Image):
        """Set buffer to value of Python Imaging Library image."""
        """Write display buffer to physical display"""
                
        imwidth, imheight = Image.size
        if imwidth != self.width or imheight != self.height:
            raise ValueError('Image must be same dimensions as display \
                ({0}x{1}).' .format(self.width, self.height))
        img = self.np.asarray(Image)
        pix = self.np.zeros((self.height,self.width,2), dtype = self.np.uint8)
        
        pix[...,[0]] = self.np.add(self.np.bitwise_and(img[...,[0]],0xF8),self.np.right_shift(img[...,[1]],5))
        pix[...,[1]] = self.np.add(self.np.bitwise_and(self.np.left_shift(img[...,[1]],3),0xE0),self.np.right_shift(img[...,[2]],3))
        
        pix = pix.flatten().tolist()
        self.SetWindows ( 0, 0, self.width, self.height)
        self.digital_write(self.DC_PIN,self.GPIO.HIGH)

        # 存储pix二进制到文件
        with open("pix.bin", "wb") as f:
            f.write(bytes(pix))

        for i in range(0,len(pix),4096):
            self.spi_writebyte(pix[i:i+4096])		
            
    def clear(self):
        """Clear contents of image buffer"""
        _buffer = [0xff]*(self.width * self.height * 2)
        self.SetWindows ( 0, 0, self.width, self.height)
        self.digital_write(self.DC_PIN,self.GPIO.HIGH)
        for i in range(0,len(_buffer),4096):
            self.spi_writebyte(_buffer[i:i+4096])	        
        

