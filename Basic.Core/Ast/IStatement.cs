namespace Basic.Core.Ast;

public interface IStatement
{
    T Accept<T>(IStatementVisitor<T> visitor);
}

public interface IStatementVisitor<T>
{
    T VisitPrintStatement(PrintStatement stmt);
    T VisitLetStatement(LetStatement stmt);
    T VisitFieldAssignStatement(FieldAssignStatement stmt);
    T VisitRemStatement(RemStatement stmt);
    T VisitGotoStatement(GotoStatement stmt);
    T VisitIfStatement(IfStatement stmt);
    T VisitBlockIfPlaceholder(BlockIfPlaceholder stmt);
    T VisitEndIfStatement(EndIfStatement stmt);
    T VisitElseIfStatement(ElseIfStatement stmt);
    T VisitElseStatement(ElseStatement stmt);
    T VisitForStatement(ForStatement stmt);
    T VisitNextStatement(NextStatement stmt);
    T VisitWhileStatement(WhileStatement stmt);
    T VisitWendStatement(WendStatement stmt);
    T VisitGosubStatement(GosubStatement stmt);
    T VisitReturnStatement(ReturnStatement stmt);
    T VisitEndStatement(EndStatement stmt);
    T VisitInputStatement(InputStatement stmt);
    T VisitDimStatement(DimStatement stmt);
    T VisitDataStatement(DataStatement stmt);
    T VisitReadStatement(ReadStatement stmt);
    T VisitRestoreStatement(RestoreStatement stmt);
    T VisitOnGotoStatement(OnGotoStatement stmt);
    T VisitSwapStatement(SwapStatement stmt);
    T VisitClsStatement(ClsStatement stmt);
    T VisitScreenStatement(ScreenStatement stmt);
    T VisitPsetStatement(PsetStatement stmt);
    T VisitLineStatement(LineStatement stmt);
    T VisitCircleStatement(CircleStatement stmt);
    T VisitPaintStatement(PaintStatement stmt);
    T VisitDrawStatement(DrawStatement stmt);
    T VisitColorStatement(ColorStatement stmt);
    T VisitLocateStatement(LocateStatement stmt);
    T VisitBeepStatement(BeepStatement stmt);

    // File I/O
    T VisitOpenStatement(OpenStatement stmt);
    T VisitCloseStatement(CloseStatement stmt);
    T VisitPrintFileStatement(PrintFileStatement stmt);
    T VisitInputFileStatement(InputFileStatement stmt);
    T VisitLineInputFileStatement(LineInputFileStatement stmt);
    T VisitWriteFileStatement(WriteFileStatement stmt);
    T VisitKillStatement(KillStatement stmt);
    T VisitNameStatement(NameStatement stmt);
    T VisitFilesStatement(FilesStatement stmt);

    // Additional statements
    T VisitRandomizeStatement(RandomizeStatement stmt);
    T VisitLineInputStatement(LineInputStatement stmt);
    T VisitDefFnStatement(DefFnStatement stmt);
    T VisitTronStatement(TronStatement stmt);
    T VisitTroffStatement(TroffStatement stmt);
    T VisitWidthStatement(WidthStatement stmt);
    T VisitSoundStatement(SoundStatement stmt);
    T VisitPlayStatement(PlayStatement stmt);
    T VisitOnErrorStatement(OnErrorStatement stmt);
    T VisitResumeStatement(ResumeStatement stmt);
    T VisitErrorStatement(ErrorStatement stmt);
    T VisitPrintUsingStatement(PrintUsingStatement stmt);

    // Random access file
    T VisitFieldStatement(FieldStatement stmt);
    T VisitGetRecordStatement(GetRecordStatement stmt);
    T VisitPutRecordStatement(PutRecordStatement stmt);
    T VisitLsetStatement(LsetStatement stmt);
    T VisitRsetStatement(RsetStatement stmt);

    // Compound statement (multiple statements on one line separated by :)
    T VisitCompoundStatement(CompoundStatement stmt);

    // QBasic-style statements
    T VisitConstStatement(ConstStatement stmt);
    T VisitSleepStatement(SleepStatement stmt);
    T VisitSelectCaseStatement(SelectCaseStatement stmt);
    T VisitCaseClauseStatement(CaseClauseStatement stmt);
    T VisitEndSelectStatement(EndSelectStatement stmt);
    T VisitDoLoopStatement(DoLoopStatement stmt);
    T VisitLoopStatement(LoopStatement stmt);
    T VisitExitStatement(ExitStatement stmt);
    T VisitDeclareStatement(DeclareStatement stmt);
    T VisitSubStatement(SubStatement stmt);
    T VisitFunctionStatement(FunctionStatement stmt);
    T VisitCallSubStatement(CallSubStatement stmt);
    T VisitLabelStatement(LabelStatement stmt);
    T VisitGotoLabelStatement(GotoLabelStatement stmt);
    T VisitGosubLabelStatement(GosubLabelStatement stmt);
    T VisitTypeStatement(TypeStatement stmt);
    T VisitTypeFieldDeclStatement(TypeFieldDeclStatement stmt);
    T VisitDefTypeStatement(DefTypeStatement stmt);
    T VisitPaletteStatement(PaletteStatement stmt);
    T VisitViewPrintStatement(ViewPrintStatement stmt);
    T VisitRedimStatement(RedimStatement stmt);
    T VisitDefSegStatement(DefSegStatement stmt);
    T VisitPokeStatement(PokeStatement stmt);
    T VisitPutGraphicsStatement(PutGraphicsStatement stmt);
    T VisitGetGraphicsStatement(GetGraphicsStatement stmt);
}
