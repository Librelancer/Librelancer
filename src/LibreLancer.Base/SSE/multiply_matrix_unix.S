    movups   (%rsi), %xmm0
    movups 16(%rsi), %xmm1
    movups 32(%rsi), %xmm2
    movups 48(%rsi), %xmm3
    movq $48, %rax                      # 2. loop reversal
1:                                      #    (for simpler exit condition)
    movss (%rdi, %rax), %xmm4           # 3. extended address operands
    shufps $0, %xmm4, %xmm4             #    (faster than pointer calculation)
    mulps %xmm0, %xmm4
    movups %xmm4, %xmm5
    movss 4(%rdi, %rax), %xmm4
    shufps $0, %xmm4, %xmm4
    mulps %xmm1, %xmm4
    addps %xmm4, %xmm5
    movss 8(%rdi, %rax), %xmm4
    shufps $0, %xmm4, %xmm4
    mulps %xmm2, %xmm4
    addps %xmm4, %xmm5
    movss 12(%rdi, %rax), %xmm4
    shufps $0, %xmm4, %xmm4
    mulps %xmm3, %xmm4
    addps %xmm4, %xmm5
    movups %xmm5, (%rdx, %rax)
    subq $16, %rax                      # one 'sub' (vs 'add' & 'cmp')
    jge 1b                              # SF=OF, idiom: jump if positive
    ret