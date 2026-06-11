
; AddTwoSum_64.asm - Chapter 3 example.


ExitProcess proto

.data
sum qword 0

.code
Main proc
	mov	  rax,12
	add	  rax,6
	mov   sum,rax

	sub rsp, 28h        ; 32 Byte Shadow Space + Alignment
	mov   rcx,sum
	call  ExitProcess
Main endp
end

