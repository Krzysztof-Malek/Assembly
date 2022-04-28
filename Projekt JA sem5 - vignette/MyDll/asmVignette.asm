.CODE

Vignette proc
	LOCAL imgWidth:DWORD, imgHeight:DWORD, remainder:QWORD, isInCircle:DWORD, fourCounter:BYTE
	push rbp
	mov	rbp, rsp
	mov r8, rcx			;initializing ptr for 1-st pixel of the image
	mov r14, rdi		;initializing ptr for last pixel of this thread

	mov rax, [r12]
	mov imgWidth, eax
	mov rax, [r12+4]
	mov imgHeight, eax

	pxor xmm7, xmm7	;emptying xmmm register
	movups xmm7, [rbx]	;initialize xmm7 register with 4 * 32-bits floating vignetteColorBGR
	pxor xmm0, xmm0
	movups xmm0, [r9]	;initialize xmm0 with 4 * 32-bits floating imgHalfWidth 
	pxor xmm1, xmm1
	movups xmm1, [r9+16]	;initialize xmm1 with 4 * 32-bits floating imgHalfHeight
	pxor xmm2, xmm2
	movups xmm2, [r9+32]	;initialize xmm2 with 4 * 32-bits floating innerCircle
	mulps  xmm2, xmm2	;get innerCircle^2
	

	;###### Calculating GreatestDistanceFromCircle and storing it in xmm3 ########################
	;halfWidth^2 + halfHeight^2 - innerCircle^2
	pxor xmm3, xmm3
	movups xmm3, xmm0
	mulps  xmm3, xmm3
	pxor xmm4, xmm4
	movups xmm4, xmm1
	mulps  xmm4, xmm4
	addps  xmm3, xmm4
	subps  xmm3, xmm2
	;############################################################################################
	
	;####################### remainder = 4 - ((imgWidth*3) % 4) #####################################
	;The additional bits can be calculated 3bytes which represents BGR * width % 4 -> (imgWidth * 3)%4
	mov remainder, 4
	mov eax, imgWidth
	mov ebx, 3
	mul ebx
	mov edx, 0
	mov ebx, 4
	div ebx
	sub remainder, rdx
	;############################################################################################

LOOPY:
	mov rax, [r12]
	mov imgWidth, eax 

	;Initialize xmm5 with 4 * 32-bits floating current imgHeight
	pxor xmm5, xmm5
	CVTSI2SS xmm5, imgHeight	;converting integer to float
	pslldq xmm5, 4
	CVTSI2SS xmm5, imgHeight
	pslldq xmm5, 4
	CVTSI2SS xmm5, imgHeight
	pslldq xmm5, 4
	CVTSI2SS xmm5, imgHeight

	subps xmm5, xmm1	;(i - halfHeight)
	mulps xmm5, xmm5	;(i - halfHeight)^2

LOOPX:
	;Initialize xmm4 with 4 * 32-bits floating current imgWidth
	mov fourCounter, 1
	pxor xmm4, xmm4
	CVTSI2SS xmm4, imgWidth
	dec imgWidth
	cmp imgWidth, 0
	je	skipRest
	pslldq xmm4, 4
	CVTSI2SS xmm4, imgWidth
	dec imgWidth
	inc fourCounter
	cmp imgWidth, 0
	je	skipRest
	pslldq xmm4, 4
	CVTSI2SS xmm4, imgWidth
	dec imgWidth
	inc fourCounter
	cmp imgWidth, 0
	je	skipRest
	pslldq xmm4, 4
	CVTSI2SS xmm4, imgWidth
	dec imgWidth
	inc fourCounter

skipRest:
	subps xmm4, xmm0	;(i - halfWidth)
	mulps xmm4, xmm4	;(i - halfWidth)^2
	addps xmm4, xmm5	;(i - halfWidth)^2 + (i - halfHeight)^2 -> distanceFromTheCenter


	movups xmm6, xmm4
	subps xmm6, xmm2	;((i - halfWidth)^2 + (i - halfHeight)^2) - innerCircle^2 -> distanceFromTheCircle
	divps xmm6, xmm3	;distanceFromTheCircle/greatestDistanceFromCircle -> percentageOfPixelChange

	vcmplt_oqps xmm11, xmm4, xmm2	;if distanceFromTheCenter < innerCircle^2 set 1
	
	;Getting proper percentage for current Pixel
forFourPixels:
	movups xmm9, xmm6	;assigning percentageOfPixelChange

	;Depending on which pixel we will calculate now we get proper percentage
	extractps eax, xmm9, 3
	pextrd isInCircle, xmm11, 3
	cmp fourCounter, 4
	je gotPercentage
	extractps eax, xmm9, 2
	pextrd isInCircle, xmm11, 2
	cmp fourCounter, 3
	je gotPercentage
	extractps eax, xmm9, 1
	pextrd isInCircle, xmm11, 1
	cmp fourCounter, 2
	je gotPercentage
	extractps eax, xmm9, 0
	pextrd isInCircle, xmm11, 0
	

gotPercentage:
	cmp isInCircle, 0
	ja skipAssigning	;if pixel is in innerCircle then skip changing RGB values
	pxor xmm9, xmm9
	pinsrd xmm9, eax, 0	;Casting 1 element on whole xmm register
	pinsrd xmm9, eax, 1
	pinsrd xmm9, eax, 2
	
	mov eax, 0				;Clearing eax
	pxor xmm8, xmm8			;Clearing xmm8
	pslldq xmm8, 4			;making space in xmm8
	mov al, byte ptr [r8+0]	;moving B value of pixel to al
	CVTSI2SS xmm8, eax		;converting integer from eax to float
	pslldq xmm8, 4
	mov al, byte ptr [r8+1]
	CVTSI2SS xmm8, eax
	pslldq xmm8, 4
	mov al, byte ptr [r8+2]
	CVTSI2SS xmm8, eax

	movups xmm10, xmm8	;copy of pixel BGR
	subps xmm10, xmm7	;Difference between BGR values from image and vignette
	mulps xmm10, xmm9	;Difference multiply by percentage of vignetteStrength
	subps xmm8, xmm10	;Making the difference bettwen two values smaller, soo we can get proper color
	cvttps2dq xmm8, xmm8

	;assigning values to pixel RGB
	pextrb rax, xmm8, 0
	pextrb byte ptr [r8+0], xmm8, 8
	pextrb rax, xmm8, 4
	pextrb byte ptr [r8+1], xmm8, 4
	pextrb rax, xmm8, 8
	pextrb byte ptr [r8+2], xmm8, 0


skipAssigning:
	add r8, 3	;moving pointer by 3
	dec fourCounter
	cmp fourCounter, 0
	jne forFourPixels	;jumping for next pixel from 4, which values are in xmm registry

	cmp imgWidth, 0
	jne LOOPX

	cmp remainder, 4	;checks if the image width is divisible by 4 - if so we don't need to manipulate pointer
	je noInc			;if the image width is not divisible by 4 we need to point further bcs bitmap complements to 4 bits
	;mov byte ptr [r8], 255
	add r8, remainder
noInc:
	inc imgHeight
	cmp r8, r14
	jb LOOPY

	pop rbp
	ret
Vignette endp

end
