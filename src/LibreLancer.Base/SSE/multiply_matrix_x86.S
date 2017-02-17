	push %ebx # save ebx register (cdecl)
	movl 8(%esp), %ebx # 1st argument
	movl 12(%esp), %ecx # 2nd argument
	movl 16(%esp), %edx # 3rd argument

    movups   (%ecx), %xmm0
    movups 16(%ecx), %xmm1
    movups 32(%ecx), %xmm2
    movups 48(%ecx), %xmm3
    movl $48, %eax                      # 2. loop reversal
1:                                      #    (for simpler exit condition)
    movss (%ebx, %eax), %xmm4           # 3. extended address operands
    shufps $0, %xmm4, %xmm4             #    (faster than pointer calculation)
    mulps %xmm0, %xmm4
    movups %xmm4, %xmm5
    movss 4(%ebx, %eax), %xmm4
    shufps $0, %xmm4, %xmm4
    mulps %xmm1, %xmm4
    addps %xmm4, %xmm5
    movss 8(%ebx, %eax), %xmm4
    shufps $0, %xmm4, %xmm4
    mulps %xmm2, %xmm4
    addps %xmm4, %xmm5
    movss 12(%ebx, %eax), %xmm4
    shufps $0, %xmm4, %xmm4
    mulps %xmm3, %xmm4
    addps %xmm4, %xmm5
    movups %xmm5, (%edx, %eax)
    subl $16, %eax                      # one 'sub' (vs 'add' & 'cmp')
    jge 1b                              # SF=OF, idiom: jump if positive
    pop %ebx # restore ebx (cdecl)
    ret