    movups   (%rdx), %xmm0
    movups 16(%rdx), %xmm1
    movups 32(%rdx), %xmm2
    movups 48(%rdx), %xmm3
    movq $48, %rax                      # 2. loop reversal
1:                                      #    (for simpler exit condition)
    movss (%rcx, %rax), %xmm4           # 3. extended address operands
    shufps $0, %xmm4, %xmm4             #    (faster than pointer calculation)
    mulps %xmm0, %xmm4
    movups %xmm4, %xmm5
    movss 4(%rcx, %rax), %xmm4
    shufps $0, %xmm4, %xmm4
    mulps %xmm1, %xmm4
    addps %xmm4, %xmm5
    movss 8(%rcx, %rax), %xmm4
    shufps $0, %xmm4, %xmm4
    mulps %xmm2, %xmm4
    addps %xmm4, %xmm5
    movss 12(%rcx, %rax), %xmm4
    shufps $0, %xmm4, %xmm4
    mulps %xmm3, %xmm4
    addps %xmm4, %xmm5
    movups %xmm5, (%r8, %rax)
    subq $16, %rax                      # one 'sub' (vs 'add' & 'cmp')
    jge 1b                              # SF=OF, idiom: jump if positive
    ret