__import__  (  "math"  )  .sqrt  (  4  )  
def multiple1  (  x,y  )  :
	return x * y
2 * 5 * 2
multiple1  (  24,multiple1  (  2,3  )    )  
def GCD  (  diviseur,dividende  )  :
	return diviseur if diviseur % dividende == 0 else GCD  (  dividende % diviseur,diviseur  )   
i = 0
u = i == 0 and False
